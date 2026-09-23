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
    public void ParseRateLimitsResponseUsesMultipleBucketsAndResetCredits()
    {
        const string json = """
        {
          "rateLimits": {
            "limitId": "codex",
            "primary": { "usedPercent": 1, "windowDurationMins": 10080, "resetsAt": 1784504087 },
            "secondary": null,
            "planType": "pro"
          },
          "rateLimitsByLimitId": {
            "codex_bengalfox": {
              "limitId": "codex_bengalfox",
              "limitName": "GPT-5.3-Codex-Spark",
              "primary": { "usedPercent": 0, "windowDurationMins": 10080, "resetsAt": 1784511024 },
              "secondary": null,
              "planType": "pro"
            },
            "codex": {
              "limitId": "codex",
              "limitName": null,
              "primary": { "usedPercent": 1, "windowDurationMins": 10080, "resetsAt": 1784504087 },
              "secondary": null,
              "planType": "pro"
            }
          },
          "rateLimitResetCredits": { "availableCount": 3 }
        }
        """;

        QuotaSnapshot snapshot = QuotaSnapshotParser.ParseRateLimitsResponse(json);
        QuotaOrbMeter orb = QuotaOrbMeter.FromSnapshot(snapshot);

        Assert.AreEqual("codex", snapshot.Primary?.LimitId);
        Assert.AreEqual(99, snapshot.Primary?.RemainingPercent);
        Assert.AreEqual("codex_bengalfox", snapshot.Secondary?.LimitId);
        Assert.AreEqual("GPT-5.3-Codex-Spark", snapshot.Secondary?.LimitName);
        Assert.AreEqual(3, snapshot.ResetCreditsAvailable);
        Assert.AreEqual("1w", orb.ShortLabel);
        Assert.AreEqual(99, orb.RemainingPercent);
        Assert.AreEqual("Codex · 7天窗口", QuotaTextFormatter.FormatWindowLabel(snapshot.Primary, "zh-CN"));
        Assert.AreEqual("Pro · 3 次可用重置", QuotaTextFormatter.FormatPlanSummary(snapshot.PlanType, snapshot.ResetCreditsAvailable, "zh-CN"));
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
    public void QuotaOrbMeterPrefersWeeklyWindowOverShorterWindow()
    {
        QuotaSnapshot snapshot = new(
            LimitId: "codex",
            LimitName: "Codex",
            PlanType: "pro",
            Primary: new QuotaWindow(UsedPercent: 27, RemainingPercent: 73, WindowDuration: TimeSpan.FromMinutes(300), ResetsAt: null),
            Secondary: new QuotaWindow(UsedPercent: 68, RemainingPercent: 32, WindowDuration: TimeSpan.FromMinutes(10080), ResetsAt: null),
            FetchedAt: DateTimeOffset.Now);

        QuotaOrbMeter orb = QuotaOrbMeter.FromSnapshot(snapshot);

        Assert.IsTrue(orb.HasData);
        Assert.AreEqual("1w", orb.ShortLabel);
        Assert.AreEqual(32, orb.RemainingPercent);
        Assert.AreEqual("32%", orb.PercentText);
    }

    [TestMethod]
    public void QuotaOrbMeterFallsBackToOnlyWindowAndClearsWithoutData()
    {
        QuotaSnapshot shortOnly = new(
            LimitId: "codex",
            LimitName: "Codex",
            PlanType: "pro",
            Primary: new QuotaWindow(UsedPercent: 55, RemainingPercent: 45, WindowDuration: TimeSpan.FromMinutes(300), ResetsAt: null),
            Secondary: null,
            FetchedAt: DateTimeOffset.Now);

        QuotaOrbMeter orb = QuotaOrbMeter.FromSnapshot(shortOnly);

        Assert.AreEqual("5h", orb.ShortLabel);
        Assert.AreEqual(45, orb.RemainingPercent);

        QuotaOrbMeter blank = QuotaOrbMeter.FromSnapshot(shortOnly with { Primary = null });

        Assert.IsFalse(blank.HasData);
        Assert.AreEqual("--", blank.ShortLabel);
        Assert.AreEqual("--", blank.PercentText);
    }

    [TestMethod]
    public void WidgetLayoutUsesPanelAndFloatingOrbModes()
    {
        WidgetWindowLayout panel = WidgetLayout.ForMode(WidgetViewMode.Panel);
        WidgetWindowLayout orb = WidgetLayout.ForMode(WidgetViewMode.FloatingOrb);

        Assert.AreEqual(460, panel.Width);
        Assert.AreEqual(292, panel.Height);
        Assert.AreEqual(320, panel.MinWidth);
        Assert.AreEqual(203, panel.MinHeight);
        Assert.IsTrue(panel.CanResize);

        Assert.AreEqual(138, orb.Width);
        Assert.AreEqual(138, orb.Height);
        Assert.AreEqual(138, orb.MinWidth);
        Assert.AreEqual(138, orb.MinHeight);
        Assert.IsFalse(orb.CanResize);
    }

    [TestMethod]
    public void DisplayAdjustmentTemporarilyReducesOnlyPrimaryQuota()
    {
        QuotaSnapshot snapshot = new(
            LimitId: "codex",
            LimitName: "Codex",
            PlanType: "pro",
            Primary: new QuotaWindow(UsedPercent: 39, RemainingPercent: 61, WindowDuration: TimeSpan.FromMinutes(300), ResetsAt: null),
            Secondary: new QuotaWindow(UsedPercent: 81, RemainingPercent: 19, WindowDuration: TimeSpan.FromMinutes(10080), ResetsAt: null),
            FetchedAt: DateTimeOffset.Now);

        QuotaDisplayAdjustment adjustment = QuotaDisplayAdjustment.PrimaryRemainingDelta(-1);
        QuotaSnapshot adjusted = adjustment.Apply(snapshot);

        Assert.AreEqual(60, adjusted.Primary?.RemainingPercent);
        Assert.AreEqual(40, adjusted.Primary?.UsedPercent);
        Assert.AreEqual(19, adjusted.Secondary?.RemainingPercent);
        Assert.AreEqual(81, adjusted.Secondary?.UsedPercent);
        Assert.AreEqual(61, snapshot.Primary?.RemainingPercent);
    }

    [TestMethod]
    public void DisplayAdjustmentCanAccumulateRepeatedPrimaryPenalties()
    {
        QuotaSnapshot snapshot = new(
            LimitId: "codex",
            LimitName: "Codex",
            PlanType: "pro",
            Primary: new QuotaWindow(UsedPercent: 3, RemainingPercent: 97, WindowDuration: TimeSpan.FromMinutes(300), ResetsAt: null),
            Secondary: new QuotaWindow(UsedPercent: 82, RemainingPercent: 18, WindowDuration: TimeSpan.FromMinutes(10080), ResetsAt: null),
            FetchedAt: DateTimeOffset.Now);

        QuotaDisplayAdjustment adjustment = QuotaDisplayAdjustment
            .PrimaryRemainingDelta(-1)
            .AddPrimaryRemainingDelta(-1);

        QuotaSnapshot adjusted = adjustment.Apply(snapshot);

        Assert.AreEqual(95, adjusted.Primary?.RemainingPercent);
        Assert.AreEqual(5, adjusted.Primary?.UsedPercent);
        Assert.AreEqual(18, adjusted.Secondary?.RemainingPercent);
    }
}
