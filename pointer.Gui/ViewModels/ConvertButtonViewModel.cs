using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace pointer.Gui.ViewModels;

public partial class ConvertButtonViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial string? ToolTip { get; set; }

    [ObservableProperty]
    public partial string Color { get; set; } = "#5933CC";

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
}
