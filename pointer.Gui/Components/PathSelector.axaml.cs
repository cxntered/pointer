using Avalonia;
using Avalonia.Controls;

namespace pointer.Gui.Components;

public partial class PathSelector : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<PathSelector, string>(nameof(Label), "Label");

    public static readonly StyledProperty<string> PathProperty =
        AvaloniaProperty.Register<PathSelector, string>(nameof(Path), string.Empty);

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

    public PathSelector()
    {
        InitializeComponent();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == LabelProperty)
            {
                LabelText.Text = Label;
            }
            else if (e.Property == PathProperty)
            {
                PathButton.Content = Path;
            }
        };
    }
}
