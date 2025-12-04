using Avalonia;
using Avalonia.Controls;

namespace pointer.Gui.Components;

public partial class Checkbox : UserControl
{
    public static readonly StyledProperty<bool?> IsCheckedProperty =
        AvaloniaProperty.Register<Checkbox, bool?>(nameof(IsChecked));

    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public Checkbox()
    {
        InitializeComponent();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == IsCheckedProperty)
            {
                InternalCheckBox.IsChecked = IsChecked;
            }
        };

        InternalCheckBox.IsCheckedChanged += (_, _) =>
        {
            IsChecked = InternalCheckBox.IsChecked;
        };
    }
}
