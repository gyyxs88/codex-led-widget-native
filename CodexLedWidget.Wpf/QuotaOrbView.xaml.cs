using System.Windows;
using System.Windows.Media;
using CodexLedWidget.Core;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace CodexLedWidget.Wpf;

public partial class QuotaOrbView : System.Windows.Controls.UserControl
{
    private const double OrbCenter = 59;
    private const double ArcRadius = 40;
    private const double GlossRadius = 34;
    private const double FullCircleThreshold = 99;

    public QuotaOrbView()
    {
        InitializeComponent();
    }

    public void Render(QuotaOrbMeter meter)
    {
        MeterLabel.Text = meter.ShortLabel;
        MeterPercent.Text = meter.PercentText;

        bool visible = meter.HasData && meter.RemainingPercent > 0;
        ProgressArc.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ProgressGloss.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        QuotaAccent accent = QuotaPalette.AccentFor(meter.HasData ? meter.RemainingPercent : 100);
        OuterRim.Stroke = new SolidColorBrush(Color.FromArgb(0x3A, accent.R, accent.G, accent.B));

        if (!visible)
        {
            return;
        }

        Color color = Color.FromRgb(accent.R, accent.G, accent.B);
        ProgressArc.Data = BuildArc(ArcRadius, meter.RemainingPercent);
        ProgressGloss.Data = BuildArc(GlossRadius, meter.RemainingPercent);
        ProgressArc.Stroke = BuildArcBrush(color);
    }

    private static Geometry BuildArc(double radius, int percent)
    {
        Point bottom = new(OrbCenter, OrbCenter + radius);
        PathFigure figure = new() { StartPoint = bottom, IsClosed = false };

        if (percent >= FullCircleThreshold)
        {
            // 起终点重合时画不出弧，整圆用两段半弧拼成。
            figure.Segments.Add(new ArcSegment(
                new Point(OrbCenter, OrbCenter - radius),
                new Size(radius, radius),
                0,
                false,
                SweepDirection.Clockwise,
                true));
            figure.Segments.Add(new ArcSegment(
                bottom,
                new Size(radius, radius),
                0,
                false,
                SweepDirection.Clockwise,
                true));
        }
        else
        {
            // 从正下方起步顺时针扫过：0% 在 6 点，25% 在 9 点（左），50% 在 12 点（上）。
            double radians = Math.PI * 2 * percent / 100.0;
            Point end = new(
                OrbCenter - (radius * Math.Sin(radians)),
                OrbCenter + (radius * Math.Cos(radians)));
            figure.Segments.Add(new ArcSegment(
                end,
                new Size(radius, radius),
                0,
                radians > Math.PI,
                SweepDirection.Clockwise,
                true));
        }

        return new PathGeometry([figure]);
    }

    private static Brush BuildArcBrush(Color color)
    {
        LinearGradientBrush brush = new()
        {
            StartPoint = new Point(0, 1),
            EndPoint = new Point(1, 0)
        };
        brush.GradientStops.Add(new GradientStop(Shift(color, 62), 0));
        brush.GradientStops.Add(new GradientStop(color, 0.55));
        brush.GradientStops.Add(new GradientStop(Shift(color, -48), 1));
        return brush;
    }

    private static Color Shift(Color color, int delta)
    {
        return Color.FromRgb(
            (byte)Math.Clamp(color.R + delta, 0, 255),
            (byte)Math.Clamp(color.G + delta, 0, 255),
            (byte)Math.Clamp(color.B + delta, 0, 255));
    }
}
