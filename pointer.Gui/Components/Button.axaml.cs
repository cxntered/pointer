namespace pointer.Gui.Components;

using System;
using Avalonia;
using Media = Avalonia.Media;

public partial class Button : Avalonia.Controls.Button
{
    public static readonly StyledProperty<string> ColorProperty =
        AvaloniaProperty.Register<Button, string>(nameof(Color), "#5933CC");

    public string Color
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
        Resources["ButtonColor"] = new Media.SolidColorBrush(Media.Color.Parse(Color));
        Resources["ButtonHoverColor"] = new Media.SolidColorBrush(CalculateHoverColor(Color));
        Resources["ButtonPressedColor"] = new Media.SolidColorBrush(CalculatePressedColor(Color));
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
