using System;
using CommunityToolkit.Mvvm.ComponentModel;
using pointer.Core.Utils;

namespace pointer.Gui.ViewModels;

public partial class PathSelectorViewModel(string label, bool isLazer) : ObservableObject
{
    [ObservableProperty]
    public partial string Path { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsValid { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    public string Label { get; } = label;
    private bool _isInitializing = true;

    public event EventHandler<string>? ValidationFailed;

    public void Initialize()
    {
        _isInitializing = true;
        Path = isLazer ? PathResolver.GetDefaultLazerPath() : PathResolver.GetDefaultStablePath();
        _isInitializing = false;
    }

    partial void OnPathChanged(string value)
    {
        IsValid = !string.IsNullOrWhiteSpace(value) &&
                  (isLazer ? PathResolver.IsValidLazerInstall(value) : PathResolver.IsValidStableInstall(value));

        if (!_isInitializing && !IsValid && !string.IsNullOrWhiteSpace(value))
        {
            var pathType = isLazer ? "osu!lazer" : "osu!stable";
            ValidationFailed?.Invoke(this, $"Please select a valid {pathType} path.");
        }
    }
}
