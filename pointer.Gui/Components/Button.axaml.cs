namespace pointer.Gui.Components;

using System;
using Avalonia;
using Avalonia.Media;

public partial class Button : Avalonia.Controls.Button
{
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<Button, Color>(nameof(Color), Color.Parse("#5933CC"));

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public Button()
    {
        UpdateButtonColors();
        PropertyChanged += (_, e) =>
        {
            if (e.Property == ColorProperty)
            {
                UpdateButtonColors();
            }
        };
    }

    private void UpdateButtonColors()
    {
        Resources["ButtonColor"] = new SolidColorBrush(Color);
        Resources["ButtonHoverColor"] = new SolidColorBrush(CalculateHoverColor(Color));
        Resources["ButtonPressedColor"] = new SolidColorBrush(CalculatePressedColor(Color));
    }

    private static Color CalculateHoverColor(Color baseColor)
    {
        byte r = (byte)Math.Min(255, baseColor.R * 1.2);
        byte g = (byte)Math.Min(255, baseColor.G * 1.2);
        byte b = (byte)Math.Min(255, baseColor.B * 1.2);
        return Color.FromRgb(r, g, b);
    }

    private static Color CalculatePressedColor(Color baseColor)
    {
        double alpha = 0.5;
        byte r = (byte)(baseColor.R * (1 - alpha) + 255 * alpha);
        byte g = (byte)(baseColor.G * (1 - alpha) + 255 * alpha);
        byte b = (byte)(baseColor.B * (1 - alpha) + 255 * alpha);
        return Color.FromRgb(r, g, b);
    }
}
