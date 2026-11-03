namespace pointer.Gui.Components;

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using pointer.Gui.Utils;
using Media = Avalonia.Media;

public partial class Button : Avalonia.Controls.Button
{
    private Border? _background;
    private Border? _hoverLayer;
    private Border? _clickLayer;
    private Triangles? _triangles;
    private Media.Color _washColor;
    private Media.Color _flashColor;
    private readonly DispatcherTimer _scaleAnimationTimer;
    private readonly DispatcherTimer _hoverLayerTimer;
    private bool _pointerPressInProgress;
    private double _scaleStart;
    private double _scaleTarget;
    private TimeSpan _scaleDuration;
    private Easing _scaleEasing = new QuinticEaseOut();
    private long _scaleAnimationStart;

    public static readonly StyledProperty<string> ColorProperty =
        AvaloniaProperty.Register<Button, string>(nameof(Color), "#5933CC");

    public string Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public Button()
    {
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        RenderTransform = new Media.ScaleTransform();

        Transitions = new Transitions
        {
            new DoubleTransition
            {
                Easing = new QuinticEaseOut(),
                Property = OpacityProperty,
                Duration = TimeSpan.FromSeconds(0.3)
            }
        };

        _scaleAnimationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _scaleAnimationTimer.Tick += UpdateScaleAnimation;

        _hoverLayerTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _hoverLayerTimer.Tick += CompleteHoverEnter;

        Click += (_, _) => ShowClickColor();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == ColorProperty || e.Property == IsEnabledProperty)
            {
                UpdateButtonColors();
            }

            if (e.Property == IsPressedProperty)
            {
                StartScaleAnimation(
                    IsPressed ? 0.9 : 1,
                    IsPressed ? TimeSpan.FromSeconds(4) : TimeSpan.FromSeconds(1),
                    IsPressed ? new QuinticEaseOut() : new OutElastic());
            }
        };
    }

    protected override void OnClick()
    {
        if (_pointerPressInProgress && !IsPointerOver)
        {
            return;
        }

        base.OnClick();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _background = e.NameScope.Find<Border>("PART_Background");
        _hoverLayer = e.NameScope.Find<Border>("PART_HoverLayer");
        _clickLayer = e.NameScope.Find<Border>("PART_ClickLayer");
        _triangles = e.NameScope.Find<Triangles>("PART_Triangles");
        if (_background is null || _hoverLayer is null || _clickLayer is null || _triangles is null)
        {
            return;
        }

        _background.Background = new Media.SolidColorBrush(Media.Color.Parse(Color)); // optimistically set color to prevent flash
        _background.Transitions = new Transitions
        {
            new BrushTransition
            {
                Easing = new QuinticEaseOut(),
                Property = Border.BackgroundProperty,
                Duration = TimeSpan.FromSeconds(0.5)
            }
        };

        _hoverLayer.Opacity = 0;
        _clickLayer.Opacity = 0.5;
        _clickLayer.Background = new Media.SolidColorBrush(Media.Colors.Transparent);

        _triangles.Transitions = new Transitions
        {
            new ColorTransition
            {
                Easing = new QuinticEaseOut(),
                Property = Triangles.GradientStartProperty,
                Duration = TimeSpan.FromSeconds(0.5)
            },
            new ColorTransition
            {
                Easing = new QuinticEaseOut(),
                Property = Triangles.GradientEndProperty,
                Duration = TimeSpan.FromSeconds(0.5)
            }
        };

        _hoverLayer.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Easing = new QuinticEaseOut(),
                Property = OpacityProperty
            },
            new BrushTransition
            {
                Easing = new QuinticEaseOut(),
                Property = Border.BackgroundProperty
            }
        };

        UpdateButtonColors();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        if (!IsEnabled)
        {
            return;
        }

        _hoverLayerTimer.Stop();
        AnimateHoverLayer(0.6, TimeSpan.FromMilliseconds(40), _flashColor);
        _hoverLayerTimer.Start();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        _hoverLayerTimer.Stop();
        AnimateHoverLayer(0, TimeSpan.FromMilliseconds(800), _washColor);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _pointerPressInProgress = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _pointerPressInProgress = false;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pointerPressInProgress = false;
    }

    private void UpdateButtonColors()
    {
        var color = Media.Color.Parse(Color);
        var dim = IsEnabled ? 1.0 : 0.5;
        var dimmed = DimColor(color, dim);
        _background?.SetValue(Border.BackgroundProperty, new Media.SolidColorBrush(dimmed));

        var lightened = Media.Color.FromRgb(
            (byte)Math.Min(color.R * 1.2, 255),
            (byte)Math.Min(color.G * 1.2, 255),
            (byte)Math.Min(color.B * 1.2, 255));
        _washColor = lightened;
        _flashColor = Media.Color.FromRgb(
            (byte)((lightened.R + 255) / 2),
            (byte)((lightened.G + 255) / 2),
            (byte)((lightened.B + 255) / 2));

        if (_hoverLayer is not null)
        {
            _hoverLayer.Background = new Media.SolidColorBrush(lightened);
        }

        if (_triangles is not null)
        {
            _triangles.GradientStart = DimColor(lightened, dim);
            _triangles.GradientEnd = dimmed;
        }

        SetValue(TextElement.ForegroundProperty, new Media.SolidColorBrush(DimColor(Media.Colors.White, dim)));
    }

    private static Media.Color DimColor(Media.Color color, double dim)
    {
        if (dim == 1.0)
            return color;

        var scalar = Math.Pow(dim, 1 / 2.2);
        byte Dim(byte c) => (byte)Math.Min(c * scalar + 0.5, 255);
        return Media.Color.FromRgb(Dim(color.R), Dim(color.G), Dim(color.B));
    }

    private void CompleteHoverEnter(object? sender, EventArgs e)
    {
        _hoverLayerTimer.Stop();

        if (IsPointerOver)
        {
            AnimateHoverLayer(0.75, TimeSpan.FromMilliseconds(800), _washColor);
        }
    }

    private void AnimateHoverLayer(double opacity, TimeSpan duration, Media.Color brush)
    {
        if (_hoverLayer?.Transitions is not { } transitions)
        {
            return;
        }

        foreach (var transition in transitions)
        {
            ((TransitionBase)transition).Duration = duration;
        }

        _hoverLayer.Background = new Media.SolidColorBrush(brush);
        _hoverLayer.Opacity = opacity;
    }

    private void ShowClickColor()
    {
        if (_clickLayer?.Background is not Media.SolidColorBrush clickBrush)
        {
            return;
        }

        clickBrush.Transitions = null;
        clickBrush.Color = Media.Colors.White;
        clickBrush.Transitions = new Transitions
        {
            new ColorTransition
            {
                Easing = new QuinticEaseOut(),
                Property = Media.SolidColorBrush.ColorProperty,
                Duration = TimeSpan.FromMilliseconds(500)
            }
        };
        clickBrush.Color = Media.Colors.Transparent;
    }

    private void StartScaleAnimation(double target, TimeSpan duration, Easing easing)
    {
        if (RenderTransform is not Media.ScaleTransform scaleTransform)
        {
            return;
        }

        _scaleAnimationTimer.Stop();
        _scaleStart = scaleTransform.ScaleX;
        _scaleTarget = target;
        _scaleDuration = duration;
        _scaleEasing = easing;
        _scaleAnimationStart = Stopwatch.GetTimestamp();

        _scaleAnimationTimer.Start();
    }

    private void UpdateScaleAnimation(object? sender, EventArgs e)
    {
        if (RenderTransform is not Media.ScaleTransform scaleTransform)
        {
            _scaleAnimationTimer.Stop();
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(_scaleAnimationStart).TotalSeconds;
        var progress = Math.Clamp(elapsed / _scaleDuration.TotalSeconds, 0, 1);
        var scale = _scaleStart + (_scaleTarget - _scaleStart) * _scaleEasing.Ease(progress);
        scaleTransform.ScaleX = scale;
        scaleTransform.ScaleY = scale;

        if (progress >= 1)
        {
            scaleTransform.ScaleX = _scaleTarget;
            scaleTransform.ScaleY = _scaleTarget;
            _scaleAnimationTimer.Stop();
        }
    }

}
