using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using pointer.Core.Models;
using pointer.Gui.Services;

namespace pointer.Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ConversionService _conversionService = new();

    public PathSelectorViewModel LazerPathSelector { get; } = new PathSelectorViewModel("osu!lazer location", isLazer: true);
    public PathSelectorViewModel StablePathSelector { get; } = new PathSelectorViewModel("osu!stable location", isLazer: false);
    public ConvertButtonViewModel ConvertButton { get; } = new ConvertButtonViewModel();

    [ObservableProperty]
    public partial bool AreControlsEnabled { get; set; }

    [ObservableProperty]
    public partial double ControlsOpacity { get; set; }

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

        ConvertButton.SetConvertAction(PerformConversionAsync);

        LazerPathSelector.Initialize();
        StablePathSelector.Initialize();
        HandlePathChange();
    }

    private void HandlePathChange()
    {
        if (LazerPathSelector.IsValid && StablePathSelector.IsValid)
        {
            _ = LoadItemCountsAsync();
        }
        else
        {
            foreach (var item in ConversionItems)
                item.Clear();

            AreControlsEnabled = false;
            ControlsOpacity = 0.5;
            UpdateButtonState();
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
                invalidPathMessage = "Please select valid installation paths";
            else if (!LazerPathSelector.IsValid)
                invalidPathMessage = "Please select a valid osu!lazer path";
            else
                invalidPathMessage = "Please select a valid osu!stable path";
        }

        ConvertButton.UpdateState(hasValidPaths, hasItemsToConvert, invalidPathMessage);
    }

    private async Task PerformConversionAsync()
    {
        try
        {
            ConvertButton.SetConverting(true);
            LazerPathSelector.IsEnabled = false;
            StablePathSelector.IsEnabled = false;
            AreControlsEnabled = false;
            ControlsOpacity = 0.5;

            await _conversionService.ConvertItemsAsync(ConversionItems);

            ConvertButton.ShowComplete();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Conversion failed: {ex.Message}");
        }
    }
}
