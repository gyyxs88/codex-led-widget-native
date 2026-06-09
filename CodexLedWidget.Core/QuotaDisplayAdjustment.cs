namespace CodexLedWidget.Core;

public sealed record QuotaDisplayAdjustment(int Delta)
{
    public static QuotaDisplayAdjustment PrimaryRemainingDelta(int delta)
    {
        return new QuotaDisplayAdjustment(delta);
    }

    public QuotaDisplayAdjustment AddPrimaryRemainingDelta(int delta)
    {
        return this with { Delta = Delta + delta };
    }

    public QuotaSnapshot Apply(QuotaSnapshot snapshot)
    {
        if (snapshot.Primary is null || Delta == 0)
        {
            return snapshot;
        }

        int remainingPercent = Math.Clamp(snapshot.Primary.RemainingPercent + Delta, 0, 100);
        QuotaWindow primary = snapshot.Primary with
        {
            RemainingPercent = remainingPercent,
            UsedPercent = 100 - remainingPercent
        };

        return snapshot with { Primary = primary };
    }
}
