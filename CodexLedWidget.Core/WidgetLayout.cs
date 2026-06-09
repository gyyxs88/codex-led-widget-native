namespace CodexLedWidget.Core;

public enum WidgetViewMode
{
    Panel,
    FloatingOrb
}

public readonly record struct WidgetWindowLayout(
    double Width,
    double Height,
    double MinWidth,
    double MinHeight,
    bool CanResize);

public static class WidgetLayout
{
    public static WidgetWindowLayout ForMode(WidgetViewMode mode)
    {
        return mode switch
        {
            WidgetViewMode.FloatingOrb => new WidgetWindowLayout(138, 138, 138, 138, false),
            _ => new WidgetWindowLayout(460, 292, 320, 203, true)
        };
    }
}
