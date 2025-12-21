using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using pointer.Core.Utils;
using pointer.Gui.Services;

namespace pointer.Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ConversionService _conversionService = new();
    private const double CONVERT_BUTTON_WIDTH = 300;

    public PathSelectorViewModel LazerPathSelector { get; } = new PathSelectorViewModel("osu!lazer location", isLazer: true);
    public PathSelectorViewModel StablePathSelector { get; } = new PathSelectorViewModel("osu!stable location", isLazer: false);
    public ConvertButtonViewModel ConvertButton { get; } = new ConvertButtonViewModel();

    [ObservableProperty]
    public partial double ConversionProgress { get; set; } = 0;

    [ObservableProperty]
    public partial double ConversionProgressRemaining { get; set; } = CONVERT_BUTTON_WIDTH;

    [ObservableProperty]
    public partial bool IsProgressVisible { get; set; } = false;

    [ObservableProperty]
    public partial string ProgressBarFill { get; set; } = "#5933CC";

    [ObservableProperty]
    public partial string ProgressBarBackground { get; set; } = "#2D1A66";

    [ObservableProperty]
    public partial string ProgressBarIcon { get; set; } = "LoaderCircle";

    [ObservableProperty]
    public partial bool AreControlsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial double ControlsOpacity { get; set; } = 100;

    [ObservableProperty]
    public partial string HardLinkIcon { get; set; } = "LoaderCircle";

    [ObservableProperty]
    public partial string HardLinkIconColor { get; set; } = "#FFFFFF";

    [ObservableProperty]
    public partial string? HardLinkToolTip { get; set; } = null;

    [ObservableProperty]
    public partial bool IsHardLinkIconVisible { get; set; } = false;

    public ObservableCollection<ConversionItemViewModel> ConversionItems { get; } =
    [
        ConversionItemViewModel.Create(ConversionItemType.Beatmaps),
        ConversionItemViewModel.Create(ConversionItemType.Scores),
        ConversionItemViewModel.Create(ConversionItemType.Skins),
        ConversionItemViewModel.Create(ConversionItemType.Collections)
    ];

    public event EventHandler<string>? ErrorOccurred;

    public MainWindowViewModel()
    {
        ConvertButton.SetConvertAction(PerformConversionAsync);

        LazerPathSelector.Initialize();
        StablePathSelector.Initialize();
        HandlePathChange();

        LazerPathSelector.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PathSelectorViewModel.Path) or nameof(PathSelectorViewModel.IsValid))
                HandlePathChange();
        };

        StablePathSelector.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PathSelectorViewModel.Path) or nameof(PathSelectorViewModel.IsValid))
                HandlePathChange();
        };

        LazerPathSelector.ValidationFailed += (_, message) => ErrorOccurred?.Invoke(this, message);
        StablePathSelector.ValidationFailed += (_, message) => ErrorOccurred?.Invoke(this, message);

        foreach (var item in ConversionItems)
        {
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ConversionItemViewModel.IsChecked) or nameof(ConversionItemViewModel.Count))
                    UpdateButtonState();
            };
        }
    }

    private void HandlePathChange()
    {
        if (LazerPathSelector.IsValid && StablePathSelector.IsValid)
        {
            _ = CheckHardLinkSupportAsync();
            _ = LoadItemCountsAsync();
        }
        else
        {
            foreach (var item in ConversionItems)
                item.Clear();

            IsHardLinkIconVisible = false;
            AreControlsEnabled = false;
            ControlsOpacity = 0.5;
            UpdateButtonState();
        }
    }

    private async Task CheckHardLinkSupportAsync()
    {
        IsHardLinkIconVisible = true;
        HardLinkIcon = "LoaderCircle";
        HardLinkIconColor = "#FFFFFF";
        HardLinkToolTip = null;

        var (hardLinkSupported, hardLinkSongsSupported) = await Task.Run(() =>
        {
            string lazerFilesPath = System.IO.Path.Combine(LazerPathSelector.Path, "files");
            string stableSongsPath = PathResolver.GetStableSongsPath(StablePathSelector.Path);
            bool linkSupported = FileLinker.IsHardLinkSupported(LazerPathSelector.Path, StablePathSelector.Path);
            bool songsSupported = FileLinker.IsHardLinkSupported(lazerFilesPath, stableSongsPath);
            return (linkSupported, songsSupported);
        });

        if (hardLinkSupported && hardLinkSongsSupported)
        {
            HardLinkIcon = "FileSymlink";
            HardLinkToolTip = "Hard linking is supported, so files won't use extra disk space. \nClick to learn more.";
        }
        else
        {
            string reason = !hardLinkSupported
                ? "installations may be on different drives or file system doesn't support it"
                : "songs folders may be on different drives or file system doesn't support it";
            HardLinkIcon = "Files";
            HardLinkIconColor = "#FFCC22";
            HardLinkToolTip = $"Hard linking is not supported ({reason}). \nFiles will be copied and use more disk space. \nClick to learn more.";
        }
    }

    private async Task LoadItemCountsAsync()
    {
        try
        {
            AreControlsEnabled = true;
            ControlsOpacity = 1.0;

            foreach (var item in ConversionItems)
                item.SetCalculating();

            ConvertButton.ToolTip = null;

            await _conversionService.LoadItemsToConvertAsync(LazerPathSelector.Path, StablePathSelector.Path, ConversionItems);

            UpdateButtonState();
        }
        catch (Exception ex)
        {
            foreach (var item in ConversionItems)
                item.Clear();

            UpdateButtonState();
            ErrorOccurred?.Invoke(this, $"Failed to load item counts: {ex.Message}");
        }
    }

    private void UpdateButtonState()
    {
        var hasValidPaths = LazerPathSelector.IsValid && StablePathSelector.IsValid;
        var hasItemsToConvert = ConversionItems.Any(item => item.IsChecked && item.Count > 0);

        string? invalidPathMessage = null;
        if (!hasValidPaths)
        {
            if (!LazerPathSelector.IsValid && !StablePathSelector.IsValid)
                invalidPathMessage = "Please select valid installation paths.";
            else if (!LazerPathSelector.IsValid)
                invalidPathMessage = "Please select a valid osu!lazer path.";
            else
                invalidPathMessage = "Please select a valid osu!stable path.";
        }

        ConvertButton.UpdateState(hasValidPaths, hasItemsToConvert, invalidPathMessage);
    }

    private async Task PerformConversionAsync()
    {
        try
        {
            ConvertButton.IsEnabled = false;
            IsProgressVisible = true;
            ProgressBarFill = "#5933CC";
            ProgressBarBackground = "#2D1A66";
            ProgressBarIcon = "LoaderCircle";
            LazerPathSelector.IsEnabled = false;
            StablePathSelector.IsEnabled = false;
            AreControlsEnabled = false;
            ControlsOpacity = 0.5;

            await _conversionService.ConvertItemsAsync(ConversionItems, new Progress<double>(p =>
            {
                ConversionProgress = p / 100 * CONVERT_BUTTON_WIDTH;
                ConversionProgressRemaining = CONVERT_BUTTON_WIDTH - ConversionProgress;
            }));

            ConvertButton.Color = "#88B300";
            ProgressBarFill = "#668800";
            ProgressBarBackground = "#334400";
            ProgressBarIcon = "Check";
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Conversion failed: {ex.Message}");
        }
    }
}
