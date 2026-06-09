namespace CodexLedWidget.Core;

public sealed record QuotaWindow(
    int UsedPercent,
    int RemainingPercent,
    TimeSpan? WindowDuration,
    DateTimeOffset? ResetsAt);

public sealed record QuotaSnapshot(
    string LimitId,
    string LimitName,
    string PlanType,
    QuotaWindow? Primary,
    QuotaWindow? Secondary,
    DateTimeOffset FetchedAt)
{
    public int? RemainingPercent => Primary?.RemainingPercent ?? Secondary?.RemainingPercent;
    public DateTimeOffset? ResetsAt => Primary?.ResetsAt ?? Secondary?.ResetsAt;
}
