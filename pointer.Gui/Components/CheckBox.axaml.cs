namespace pointer.Gui.Components;

using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using pointer.Gui.Utils;
using Media = Avalonia.Media;

public class CheckBox : Avalonia.Controls.CheckBox
{
    private Border? _nub;
    private Media.Color _washColor;
    private Media.Color _flashColor;
    private readonly DispatcherTimer _hoverSettleTimer;
    private TopLevel? _topLevel;
    private bool _isHovered;

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

        Click += (_, _) => SoundEffects.Play(IsChecked == true ? "check-on" : "check-off");

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

        _nub.Transitions = new Transitions
        {
            new BrushTransition
            {
                Easing = new QuinticEaseOut(),
                Property = Border.BackgroundProperty
            },
            new BrushTransition
            {
                Easing = new QuinticEaseOut(),
                Property = Border.BorderBrushProperty
            }
        };

        UpdateNubColors();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // avalonia doesn't hit-test disabled controls, so we have to track from the top level
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(PointerMovedEvent, OnTopLevelPointerMoved, RoutingStrategies.Tunnel, true);
        if (_topLevel is not null)
        {
            _topLevel.PointerExited += OnTopLevelPointerExited;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(PointerMovedEvent, OnTopLevelPointerMoved);
        if (_topLevel is not null)
        {
            _topLevel.PointerExited -= OnTopLevelPointerExited;
        }
        _topLevel = null;
        _isHovered = false;

        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        SetHovered(true);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        SetHovered(!IsEffectivelyEnabled && IsPointerWithinBounds(e)); // keep hover when disabled and pointer is still inside
    }

    private void OnTopLevelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (IsEffectivelyEnabled)
        {
            return;
        }

        SetHovered(IsPointerWithinBounds(e));
    }

    private void OnTopLevelPointerExited(object? sender, PointerEventArgs e)
    {
        SetHovered(IsPointerWithinBounds(e)); // clear hover only when pointer actually leaves
    }

    private bool IsPointerWithinBounds(PointerEventArgs e)
    {
        return new Rect(Bounds.Size).Contains(e.GetPosition(this));
    }

    private void SetHovered(bool hovered)
    {
        if (_isHovered == hovered)
        {
            return;
        }

        _isHovered = hovered;
        _hoverSettleTimer.Stop();

        if (hovered)
        {
            SoundEffects.Play("default-hover");
            AnimateNubColor(_flashColor, TimeSpan.FromMilliseconds(40));
            _hoverSettleTimer.Start();
        }
        else
        {
            AnimateNubColor(RestingColor(), TimeSpan.FromMilliseconds(800));
        }
    }

    private void CompleteHoverEnter(object? sender, EventArgs e)
    {
        _hoverSettleTimer.Stop();

        if (_isHovered)
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
        if (_nub?.Transitions is not { } transitions)
        {
            return;
        }

        foreach (var transition in transitions)
        {
            ((TransitionBase)transition).Duration = duration;
        }

        _nub.Background = new Media.SolidColorBrush(IsChecked == true ? color : Media.Colors.Transparent);
        _nub.BorderBrush = new Media.SolidColorBrush(color);
    }

    private Media.Color RestingColor()
    {
        return _isHovered ? _washColor : Media.Color.Parse(Accent);
    }

    private static Media.Color Lighten(Media.Color color, double amount)
    {
        byte Lift(byte channel) => (byte)Math.Min(channel * amount, 255);
        return Media.Color.FromRgb(Lift(color.R), Lift(color.G), Lift(color.B));
    }
}
