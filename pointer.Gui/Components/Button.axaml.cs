namespace pointer.Gui.Components;

using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Media = Avalonia.Media;

public partial class Button : Avalonia.Controls.Button
{
    private Border? _background;

    public static readonly StyledProperty<string> ColorProperty =
        AvaloniaProperty.Register<Button, string>(nameof(Color), "#5933CC");

    public string Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public Button()
    {
        Transitions = new Transitions
        {
            new DoubleTransition
            {
                Easing = new QuinticEaseOut(),
                Property = OpacityProperty,
                Duration = TimeSpan.FromSeconds(0.3)
            }
        };

        UpdateButtonColors();
        PropertyChanged += (_, e) =>
        {
            if (e.Property == ColorProperty ||
                e.Property == IsEnabledProperty ||
                e.Property == IsPressedProperty)
            {
                UpdateButtonColors();
            }
        };
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _background = e.NameScope.Find<Border>("PART_Background");
        if (_background is null)
        {
            return;
        }

        _background.Transitions = new Transitions
        {
            new BrushTransition
            {
                Property = Border.BackgroundProperty,
                Duration = TimeSpan.FromSeconds(0.3)
            }
        };

        UpdateButtonColors();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        UpdateButtonColors();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        UpdateButtonColors();
    }

    private void UpdateButtonColors()
    {
        if (_background is null)
        {
            return;
        }

        _background.Background = new Media.SolidColorBrush(GetButtonColor());
    }

    private Media.Color GetButtonColor()
    {
        if (!IsEnabled)
        {
            return Media.Color.Parse(Color);
        }

        if (IsPressed)
        {
            return CalculatePressedColor(Color);
        }

        if (IsPointerOver)
        {
            return CalculateHoverColor(Color);
        }

        return Media.Color.Parse(Color);
    }

    private static Media.Color CalculateHoverColor(string color)
    {
        var baseColor = Media.Color.Parse(color);
        byte r = (byte)Math.Min(255, baseColor.R * 1.2);
        byte g = (byte)Math.Min(255, baseColor.G * 1.2);
        byte b = (byte)Math.Min(255, baseColor.B * 1.2);
        return Media.Color.FromRgb(r, g, b);
    }

    private static Media.Color CalculatePressedColor(string color)
    {
        var baseColor = Media.Color.Parse(color);
        double alpha = 0.5;
        byte r = (byte)(baseColor.R * (1 - alpha) + 255 * alpha);
        byte g = (byte)(baseColor.G * (1 - alpha) + 255 * alpha);
        byte b = (byte)(baseColor.B * (1 - alpha) + 255 * alpha);
        return Media.Color.FromRgb(r, g, b);
    }
}
