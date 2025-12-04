using System;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace pointer.Gui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

public class OutElasticHalf : Easing
{
    private const double ELASTIC_CONST = 2 * Math.PI / .3;
    private const double ELASTIC_CONST2 = .3 / 4;
    private static readonly double elastic_offset_half = Math.Pow(2, -10) * Math.Sin((.5 - ELASTIC_CONST2) * ELASTIC_CONST);

    public override double Ease(double progress)
    {
        return Math.Pow(2, -10 * progress) * Math.Sin((.5 * progress - ELASTIC_CONST2) * ELASTIC_CONST) + 1 - elastic_offset_half * progress;
    }
}