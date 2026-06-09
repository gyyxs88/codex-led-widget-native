namespace CodexLedWidget.Core;

public sealed record DualQuotaMeterSegment(string ShortLabel, int RemainingPercent, bool HasData)
{
    public string PercentText => HasData ? $"{RemainingPercent}%" : "--";
}

public sealed record DualQuotaMeter(DualQuotaMeterSegment Left, DualQuotaMeterSegment Right)
{
    public static DualQuotaMeter FromSnapshot(QuotaSnapshot snapshot, string cultureName)
    {
        return new DualQuotaMeter(
            CreateSegment(snapshot.Primary, "5h"),
            CreateSegment(snapshot.Secondary, "1w"));
    }

    private static DualQuotaMeterSegment CreateSegment(QuotaWindow? window, string label)
    {
        if (window is null)
        {
            return new DualQuotaMeterSegment(label, 0, false);
        }

        return new DualQuotaMeterSegment(label, Math.Clamp(window.RemainingPercent, 0, 100), true);
    }
}
