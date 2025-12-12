using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace pointer.Gui.ViewModels;

public partial class ConvertButtonViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Text { get; set; } = "point items back to stable!";

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial string? ToolTip { get; set; }

    private Func<Task>? _convertAction;

    public void SetConvertAction(Func<Task> action)
    {
        _convertAction = action;
    }

    public void UpdateState(bool hasValidPaths, bool hasItemsToConvert, string? invalidPathMessage = null)
    {
        if (!hasValidPaths)
        {
            IsEnabled = false;
            ToolTip = invalidPathMessage ?? "Please select valid installation paths.";
            return;
        }

        IsEnabled = hasItemsToConvert;
        ToolTip = hasItemsToConvert ? null : "No items to convert.";
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (_convertAction != null)
        {
            await _convertAction();
        }
    }

    public void SetConverting(bool isConverting)
    {
        IsEnabled = !isConverting;
        Text = isConverting ? "converting..." : "point items back to stable!";
        ToolTip = isConverting ? "converting..." : ToolTip;
    }

    public void ShowComplete()
    {
        IsEnabled = false;
        Text = "conversion complete!";
    }
}
