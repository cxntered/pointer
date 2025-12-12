using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace pointer.Gui.Components;

public partial class PathSelector : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<PathSelector, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> PathProperty =
        AvaloniaProperty.Register<PathSelector, string>(nameof(Path), string.Empty);

    public static readonly StyledProperty<bool> IsValidProperty =
        AvaloniaProperty.Register<PathSelector, bool>(nameof(IsValid), true);

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public bool IsValid
    {
        get => GetValue(IsValidProperty);
        set => SetValue(IsValidProperty, value);
    }

    public PathSelector()
    {
        InitializeComponent();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == LabelProperty)
            {
                LabelText.Text = Label;
            }
            else if (e.Property == PathProperty || e.Property == IsValidProperty)
            {
                if (!IsValid)
                {
                    PathButton.Content = $"Click to locate {Label}";
                    PathButton.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#B4B4B4"));
                }
                else
                {
                    PathButton.Content = Path;
                    PathButton.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
                }
            }
            else if (e.Property == IsEnabledProperty)
            {
                PathButton.IsEnabled = IsEnabled;
                PathButton.Opacity = IsEnabled ? 1.0 : 0.5;
            }
        };

        PathButton.Click += OnPathButtonClick;
    }

    private async void OnPathButtonClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = $"Select {Label}",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            // trim trailing slashes for consistency :3
            Path = folders[0].Path.LocalPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        }
    }
}
