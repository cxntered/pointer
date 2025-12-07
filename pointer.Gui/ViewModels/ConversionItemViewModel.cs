using CommunityToolkit.Mvvm.ComponentModel;

namespace pointer.Gui.ViewModels;

public partial class ConversionItemViewModel(string name) : ObservableObject
{
    [ObservableProperty]
    public partial string DisplayText { get; set; } = name;

    [ObservableProperty]
    public partial bool IsChecked { get; set; } = true;

    [ObservableProperty]
    public partial int Count { get; set; }

    public string Name { get; } = name;

    partial void OnCountChanged(int value)
    {
        DisplayText = $"{Name} ({value} items)";
    }

    public void SetCalculating()
    {
        DisplayText = $"{Name} (calculating...)";
    }
}
