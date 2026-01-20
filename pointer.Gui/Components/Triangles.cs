// This file is derived from osu!'s TrianglesV2, ported to Avalonia.
// Original source: https://github.com/ppy/osu/blob/b1131ffd23b101246b6a8ebccd88dcecf280de3f/osu.Game/Graphics/Backgrounds/TrianglesV2.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace pointer.Gui.Components;

public class Triangles : Control
{
    public Triangles()
    {
        Transitions = new Transitions
        {
            new ColorTransition
            {
                Property = GradientStartProperty,
                Duration = TimeSpan.FromSeconds(0.3)
            },
            new ColorTransition
            {
                Property = GradientEndProperty,
                Duration = TimeSpan.FromSeconds(0.3)
            }
        };
    }

    private const double TriangleSize = 100.0;
    private const double BaseVelocity = 50.0;
    private const double EquilateralRatio = 0.866; // sqrt(3) / 2

    private const double Thickness = 0.02;
    private const double SpawnRatio = 0.6;
    private const double Velocity = 1.0;

    private readonly List<TriangleParticle> _particles = [];
    private readonly Stopwatch _stopwatch = new();
    private DispatcherTimer? _animationTimer;
    private long _lastFrameTicks;

    public static readonly StyledProperty<Color> GradientStartProperty =
        AvaloniaProperty.Register<Triangles, Color>(
            nameof(GradientStart),
            Color.FromRgb(105, 60, 240));

    public static readonly StyledProperty<Color> GradientEndProperty =
        AvaloniaProperty.Register<Triangles, Color>(
            nameof(GradientEnd),
            Color.FromRgb(89, 51, 204));

    public Color GradientStart
    {
        get => GetValue(GradientStartProperty);
        set => SetValue(GradientStartProperty, value);
    }

    public Color GradientEnd
    {
        get => GetValue(GradientEndProperty);
        set => SetValue(GradientEndProperty, value);
    }

    static Triangles()
    {
        AffectsRender<Triangles>(GradientStartProperty, GradientEndProperty);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _particles.Clear();
        _stopwatch.Restart();
        _lastFrameTicks = _stopwatch.ElapsedTicks;
        PopulateTriangles(randomY: true);

        _animationTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            DispatcherPriority.Render,
            OnAnimationTick);
        _animationTimer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _animationTimer?.Stop();
        _animationTimer = null;
        _stopwatch.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        long currentTicks = _stopwatch.ElapsedTicks;
        double elapsed = (currentTicks - _lastFrameTicks) / (double)Stopwatch.Frequency;
        _lastFrameTicks = currentTicks;

        if (elapsed <= 0 || Bounds.Height <= 0)
            return;

        double movedDistance = -elapsed * Velocity * BaseVelocity / Bounds.Height;

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle = particle with
            {
                RelativeY = particle.RelativeY + Math.Max(0.5, particle.SpeedMultiplier) * movedDistance
            };

            double bottomPos = particle.RelativeY + TriangleSize * EquilateralRatio / Bounds.Height;
            if (bottomPos < 0)
            {
                _particles.RemoveAt(i);
            }
            else
            {
                _particles[i] = particle;
            }
        }

        PopulateTriangles(randomY: false);
        InvalidateVisual();
    }

    private void PopulateTriangles(bool randomY)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        int targetCount = (int)Math.Clamp(Bounds.Width * 0.02 * SpawnRatio, 1, 8192);

        while (_particles.Count < targetCount)
        {
            _particles.Add(CreateParticle(randomY));
        }
    }

    private TriangleParticle CreateParticle(bool randomY)
    {
        const double stdDev = 0.16;
        const double mean = 0.5;

        // box-muller transform for normal distribution
        double u1 = 1.0 - Random.Shared.NextDouble();
        double u2 = 1.0 - Random.Shared.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        double speedMultiplier = Math.Max(mean + stdDev * randStdNormal, 0.1);

        double y = 1.0;
        if (randomY && Bounds.Height > 0)
        {
            double maxOffset = TriangleSize * EquilateralRatio / Bounds.Height;
            y = double.Lerp(-maxOffset, 1.0, Random.Shared.NextDouble());
        }

        return new TriangleParticle(
            RelativeX: Random.Shared.NextDouble(),
            RelativeY: y,
            SpeedMultiplier: speedMultiplier
        );
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        using (context.PushClip(bounds))
        {
            const double triWidth = TriangleSize;
            const double triHeight = TriangleSize * EquilateralRatio;

            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
                EndPoint = new RelativePoint(0, bounds.Height, RelativeUnit.Absolute),
                GradientStops =
                [
                    new(GradientStart, 0.0),
                    new(GradientEnd, 1.0)
                ]
            };
            var pen = new Pen(
                gradientBrush,
                Math.Max(1.0, Thickness * triHeight),
                lineCap: PenLineCap.Round,
                lineJoin: PenLineJoin.Round);

            foreach (var particle in _particles)
            {
                DrawTriangle(context, particle, bounds, triWidth, triHeight, pen);
            }
        }
    }

    private static void DrawTriangle(DrawingContext context, TriangleParticle particle,
        Rect bounds, double triWidth, double triHeight, Pen pen)
    {
        double topX = particle.RelativeX * bounds.Width;
        double topY = particle.RelativeY * bounds.Height;

        var p0 = new Point(topX, topY);
        var p1 = new Point(topX - triWidth / 2.0, topY + triHeight);
        var p2 = new Point(topX + triWidth / 2.0, topY + triHeight);

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(p0, isFilled: false);
            geometryContext.LineTo(p1);
            geometryContext.LineTo(p2);
            geometryContext.EndFigure(true);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private record TriangleParticle(double RelativeX, double RelativeY, double SpeedMultiplier);
}
