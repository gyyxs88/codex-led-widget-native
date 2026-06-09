using CodexLedWidget.Core;
using Media = System.Windows.Media;

namespace CodexLedWidget.Wpf;

public partial class QuotaOrbView : System.Windows.Controls.UserControl
{
    public QuotaOrbView()
    {
        InitializeComponent();
    }

    public void Render(DualQuotaMeter meter)
    {
        PrimaryMeterLabel.Text = meter.Left.ShortLabel;
        PrimaryMeterPercent.Text = meter.Left.PercentText;
        SecondaryMeterLabel.Text = meter.Right.ShortLabel;
        SecondaryMeterPercent.Text = meter.Right.PercentText;
        PrimaryHalfFill.Height = FillHeight(meter.Left);
        SecondaryHalfFill.Height = FillHeight(meter.Right);
        PrimaryHalfFill.Background = PrimaryFillBrush(meter.Left);
        SecondaryHalfFill.Background = SecondaryFillBrush(meter.Right);
    }

    private static double FillHeight(DualQuotaMeterSegment segment)
    {
        if (!segment.HasData)
        {
            return 0;
        }

        return Math.Max(5, Math.Min(118, segment.RemainingPercent / 100.0 * 118));
    }

    private static Media.Brush PrimaryFillBrush(DualQuotaMeterSegment segment)
    {
        if (!segment.HasData)
        {
            return Media.Brushes.Transparent;
        }

        Media.LinearGradientBrush brush = new()
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 1)
        };
        brush.GradientStops.Add(new Media.GradientStop(Media.Color.FromArgb(220, 90, 245, 184), 0));
        brush.GradientStops.Add(new Media.GradientStop(Media.Color.FromArgb(238, 24, 203, 139), 0.5));
        brush.GradientStops.Add(new Media.GradientStop(Media.Color.FromArgb(245, 12, 150, 132), 1));
        return brush;
    }

    private static Media.Brush SecondaryFillBrush(DualQuotaMeterSegment segment)
    {
        if (!segment.HasData)
        {
            return Media.Brushes.Transparent;
        }

        Media.LinearGradientBrush brush = new()
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 1)
        };
        brush.GradientStops.Add(new Media.GradientStop(Media.Color.FromArgb(220, 74, 225, 255), 0));
        brush.GradientStops.Add(new Media.GradientStop(Media.Color.FromArgb(238, 27, 141, 255), 0.52));
        brush.GradientStops.Add(new Media.GradientStop(Media.Color.FromArgb(230, 107, 66, 232), 1));
        return brush;
    }
}
