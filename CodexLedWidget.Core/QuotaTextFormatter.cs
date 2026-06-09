using System.Globalization;

namespace CodexLedWidget.Core;

public static class QuotaTextFormatter
{
    public static string FormatWindow(QuotaWindow? window, string cultureName)
    {
        if (window is null)
        {
            return "--";
        }

        string remainingLabel = cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "剩余" : "left";
        string resetLabel = cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "重置" : "resets";
        List<string> parts = [$"{window.RemainingPercent}% {remainingLabel}"];

        if (window.ResetsAt is DateTimeOffset resetsAt)
        {
            parts.Add($"{resetLabel} {FormatResetDateTime(resetsAt, cultureName)}");
        }

        return string.Join(" · ", parts);
    }

    public static string FormatResetDateTime(DateTimeOffset dateTime, string cultureName)
    {
        if (cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return $"{dateTime.Month}月{dateTime.Day}日 {dateTime:HH\\:mm}";
        }

        CultureInfo culture = CultureInfo.GetCultureInfo("en-US");
        return $"{dateTime.ToString("MMM d", culture)} {dateTime:HH\\:mm}";
    }

    public static string FormatPlan(string? planType)
    {
        if (string.IsNullOrWhiteSpace(planType) || planType.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return "--";
        }

        string lower = planType.Trim().ToLowerInvariant();
        return string.Concat(char.ToUpperInvariant(lower[0]), lower[1..]);
    }
}
