using System;
using Avalonia.Animation.Easings;

namespace pointer.Gui.Utils;

public class OutElastic : Easing
{
    private const double ELASTIC_CONST = 2 * Math.PI / 0.3;
    private const double ELASTIC_CONST2 = 0.3 / 4;
    private static readonly double elastic_offset_full = Math.Pow(2, -11);

    public override double Ease(double progress)
    {
        return Math.Pow(2, -10 * progress) * Math.Sin((progress - ELASTIC_CONST2) * ELASTIC_CONST) + 1 - elastic_offset_full * progress;
    }
}