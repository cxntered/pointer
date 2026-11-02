// This file is derived from osu!'s LoadingSpinner, ported to Avalonia.
// Original source: https://github.com/ppy/osu/blob/4e9aa126a2c1f5575fa6f31519932f4ce6ed9f2d/osu.Game/Graphics/UserInterface/LoadingSpinner.cs
//
// Copyright (c) 2025 ppy Pty Ltd <contact@ppy.sh>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using IconPacks.Avalonia.Lucide;

namespace pointer.Gui.Components;

public class LoadingSpinner : Decorator
{
    private const double spin_duration = 3150;
    private const double beat_length = 1000; // fixed at 60 bpm as we don't implement beat sync

    private static readonly QuarticEaseInOut easing = new();

    private readonly RotateTransform continuousSpin = new();
    private readonly RotateTransform steppedSpin = new();

    private DispatcherTimer? timer;
    private long lastFrame;
    private double continuousAngle;
    private double stepStartAngle;
    private double beatElapsed;

    public LoadingSpinner()
    {
        RenderTransform = steppedSpin;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (Child != null)
        {
            Child.RenderTransform = continuousSpin;
            Child.PropertyChanged += OnChildPropertyChanged;
        }

        UpdateAnimationState();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Child != null)
            Child.PropertyChanged -= OnChildPropertyChanged;

        Stop();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsVisibleProperty)
            UpdateAnimationState();
    }

    private void OnChildPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == PackIconLucide.KindProperty)
            UpdateAnimationState();
    }

    private bool ShouldAnimate => IsVisible && Child is PackIconLucide { Kind: PackIconLucideKind.LoaderCircle };

    private void UpdateAnimationState()
    {
        if (!ShouldAnimate)
        {
            Stop();
            return;
        }

        if (timer != null)
            return;

        continuousAngle = 0;
        stepStartAngle = 0;
        beatElapsed = 0;
        continuousSpin.Angle = 0;
        steppedSpin.Angle = 0;
        lastFrame = Stopwatch.GetTimestamp();

        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, OnTick);
        timer.Start();
    }

    private void Stop()
    {
        timer?.Stop();
        timer = null;

        continuousSpin.Angle = 0;
        steppedSpin.Angle = 0;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        double elapsed = (now - lastFrame) * 1000d / Stopwatch.Frequency;
        lastFrame = now;

        continuousAngle = (continuousAngle + 360 * elapsed / spin_duration) % 360;
        continuousSpin.Angle = continuousAngle;

        beatElapsed += elapsed;

        while (beatElapsed >= beat_length)
        {
            beatElapsed -= beat_length;
            stepStartAngle += 90;
        }

        steppedSpin.Angle = stepStartAngle + 90 * easing.Ease(beatElapsed / beat_length);
    }
}
