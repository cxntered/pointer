using Avalonia;
using Avalonia.Controls;

namespace pointer.Gui.Components;

public partial class Text : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<Text, string>(nameof(Content), string.Empty);

    public new string Content
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Text()
    {
        InitializeComponent();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == TextProperty)
            {
                InternalTextBlock.Text = Content;
            }
        };
    }
}
