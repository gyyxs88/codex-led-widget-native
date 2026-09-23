namespace CodexLedWidget.Core;

/// <summary>球体只呈现一个额度：优先周窗口，取不到时回退到任意可用窗口。</summary>
public sealed record QuotaOrbMeter(string ShortLabel, int RemainingPercent, bool HasData)
{
    private static readonly TimeSpan WeeklyWindow = TimeSpan.FromDays(7);

    public string PercentText => HasData ? $"{RemainingPercent}%" : "--";

    public static QuotaOrbMeter FromSnapshot(QuotaSnapshot snapshot)
    {
        QuotaWindow? window = SelectWeeklyWindow(snapshot);
        if (window is null)
        {
            return new QuotaOrbMeter("--", 0, false);
        }

        return new QuotaOrbMeter(
            QuotaTextFormatter.FormatWindowShortLabel(window),
            Math.Clamp(window.RemainingPercent, 0, 100),
            true);
    }

    private static QuotaWindow? SelectWeeklyWindow(QuotaSnapshot snapshot)
    {
        return AsWeekly(snapshot.Primary)
            ?? AsWeekly(snapshot.Secondary)
            ?? snapshot.Primary
            ?? snapshot.Secondary;
    }

    private static QuotaWindow? AsWeekly(QuotaWindow? window)
    {
        return window?.WindowDuration >= WeeklyWindow ? window : null;
    }
}
