using CodexLedWidget.Core;
using Media = System.Windows.Media;

namespace CodexLedWidget.Wpf;

public partial class QuotaOrbView : System.Windows.Controls.UserControl
{
    private const double OrbDiameter = 118;

    public QuotaOrbView()
    {
        InitializeComponent();
    }

    public void Render(QuotaOrbMeter meter)
    {
        MeterLabel.Text = meter.ShortLabel;
        MeterPercent.Text = meter.PercentText;
        OrbFill.Height = FillHeight(meter);
        OrbFill.Background = FillBrush(meter);
    }

    private static double FillHeight(QuotaOrbMeter meter)
    {
        if (!meter.HasData)
        {
            return 0;
        }

        return Math.Max(5, Math.Min(OrbDiameter, meter.RemainingPercent / 100.0 * OrbDiameter));
    }

    private static Media.Brush FillBrush(QuotaOrbMeter meter)
    {
        if (!meter.HasData)
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
}
