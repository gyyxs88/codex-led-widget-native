using CodexLedWidget.Core;

namespace CodexLedWidget.Tests;

[TestClass]
public sealed class QuotaSnapshotTests
{
    [TestMethod]
    public void ParseRateLimitsResponseNormalizesCodexWindows()
    {
        const string json = """
        {
          "rateLimitsByLimitId": {
            "codex": {
              "limitId": "codex",
              "limitName": "Codex",
              "planType": "pro",
              "primary": {
                "usedPercent": 14,
                "windowDurationMins": 300,
                "resetsAt": 1780930980
              },
              "secondary": {
                "usedPercent": 66,
                "windowDurationMins": 10080,
                "resetsAt": 1781153370
              }
            }
          }
        }
        """;

        QuotaSnapshot snapshot = QuotaSnapshotParser.ParseRateLimitsResponse(json);

        Assert.AreEqual("pro", snapshot.PlanType);
        Assert.AreEqual(86, snapshot.Primary?.RemainingPercent);
        Assert.AreEqual(34, snapshot.Secondary?.RemainingPercent);
        Assert.AreEqual(TimeSpan.FromMinutes(300), snapshot.Primary?.WindowDuration);
        Assert.AreEqual(TimeSpan.FromMinutes(10080), snapshot.Secondary?.WindowDuration);
    }

    [TestMethod]
    public void FormatterShowsRemainingAndChineseResetTimeWithoutUsedPercent()
    {
        QuotaWindow window = new(
            UsedPercent: 14,
            RemainingPercent: 86,
            WindowDuration: TimeSpan.FromMinutes(300),
            ResetsAt: new DateTimeOffset(2026, 6, 8, 19, 3, 0, TimeSpan.FromHours(8)));

        string text = QuotaTextFormatter.FormatWindow(window, "zh-CN");

        Assert.AreEqual("86% 剩余 · 重置 6月8日 19:03", text);
        Assert.IsFalse(text.Contains("已用", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("14%", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DualQuotaMeterUsesPrimaryForLeftAndSecondaryForRight()
    {
        QuotaSnapshot snapshot = new(
            LimitId: "codex",
            LimitName: "Codex",
            PlanType: "pro",
            Primary: new QuotaWindow(UsedPercent: 27, RemainingPercent: 73, WindowDuration: TimeSpan.FromMinutes(300), ResetsAt: null),
            Secondary: new QuotaWindow(UsedPercent: 68, RemainingPercent: 32, WindowDuration: TimeSpan.FromMinutes(10080), ResetsAt: null),
            FetchedAt: DateTimeOffset.Now);

        DualQuotaMeter meter = DualQuotaMeter.FromSnapshot(snapshot, "zh-CN");

        Assert.AreEqual("5h", meter.Left.ShortLabel);
        Assert.AreEqual(73, meter.Left.RemainingPercent);
        Assert.AreEqual("1w", meter.Right.ShortLabel);
        Assert.AreEqual(32, meter.Right.RemainingPercent);
    }
}
