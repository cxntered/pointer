namespace pointer.Gui.Components;

using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Media = Avalonia.Media;

public class CheckBox : Avalonia.Controls.CheckBox
{
    private Border? _nub;
    private Media.Color _washColor;
    private Media.Color _flashColor;
    private readonly DispatcherTimer _hoverSettleTimer;

    public static readonly StyledProperty<string> AccentProperty =
        AvaloniaProperty.Register<CheckBox, string>(nameof(Accent), "#8C66FF");

    public string Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public CheckBox()
    {
        _hoverSettleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _hoverSettleTimer.Tick += CompleteHoverEnter;

        PropertyChanged += (_, e) =>
        {
            if (e.Property == AccentProperty || e.Property == IsCheckedProperty)
            {
                UpdateNubColors();
            }
        };
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _nub = e.NameScope.Find<Border>("PART_Nub");
        if (_nub is null)
        {
            return;
        }

        UpdateNubColors();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        if (!IsEnabled)
        {
            return;
        }

        _hoverSettleTimer.Stop();
        AnimateNubColor(_flashColor, TimeSpan.FromMilliseconds(40));
        _hoverSettleTimer.Start();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        _hoverSettleTimer.Stop();
        AnimateNubColor(RestingColor(), TimeSpan.FromMilliseconds(800));
    }

    private void CompleteHoverEnter(object? sender, EventArgs e)
    {
        _hoverSettleTimer.Stop();

        if (IsPointerOver)
        {
            AnimateNubColor(_washColor, TimeSpan.FromMilliseconds(800));
        }
    }

    private void UpdateNubColors()
    {
        var accent = Media.Color.Parse(Accent);
        var lightened = Lighten(accent, 1.2);
        _washColor = lightened;
        _flashColor = Lighten(lightened, 1.5);

        if (_nub is not null)
        {
            AnimateNubColor(RestingColor(), TimeSpan.FromMilliseconds(300));
        }
    }

    private void AnimateNubColor(Media.Color color, TimeSpan duration)
    {
        if (_nub is null)
        {
            return;
        }

        _nub.Transitions = new Transitions
        {
            new BrushTransition
            {
                Easing = new QuinticEaseOut(),
                Property = Border.BackgroundProperty,
                Duration = duration
            },
            new BrushTransition
            {
                Easing = new QuinticEaseOut(),
                Property = Border.BorderBrushProperty,
                Duration = duration
            }
        };
        _nub.Background = new Media.SolidColorBrush(IsChecked == true ? color : Media.Colors.Transparent);
        _nub.BorderBrush = new Media.SolidColorBrush(color);
    }

    private Media.Color RestingColor()
    {
        return IsPointerOver && IsEnabled ? _washColor : Media.Color.Parse(Accent);
    }

    private static Media.Color Lighten(Media.Color color, double amount)
    {
        byte Lift(byte channel) => (byte)Math.Min(channel * amount, 255);
        return Media.Color.FromRgb(Lift(color.R), Lift(color.G), Lift(color.B));
    }
}
