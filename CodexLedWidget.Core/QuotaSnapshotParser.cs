using System.Text.Json.Nodes;

namespace CodexLedWidget.Core;

public static class QuotaSnapshotParser
{
    public static QuotaSnapshot ParseRateLimitsResponse(string json)
    {
        JsonNode root = JsonNode.Parse(json) ?? throw new InvalidOperationException("Codex 返回了空响应。");
        JsonNode snapshotNode = ResolveSnapshotNode(root)
            ?? throw new InvalidOperationException("Codex 响应中没有额度窗口。");

        return new QuotaSnapshot(
            LimitId: ReadString(snapshotNode, "limitId") ?? "codex",
            LimitName: ReadString(snapshotNode, "limitName") ?? "Codex",
            PlanType: ReadString(snapshotNode, "planType") ?? "unknown",
            Primary: ParseWindow(snapshotNode["primary"]),
            Secondary: ParseWindow(snapshotNode["secondary"]),
            FetchedAt: DateTimeOffset.Now);
    }

    private static JsonNode? ResolveSnapshotNode(JsonNode root)
    {
        JsonNode? byId = root["rateLimitsByLimitId"];
        if (byId is JsonObject map)
        {
            if (map.TryGetPropertyValue("codex", out JsonNode? codex) && codex is not null)
            {
                return codex;
            }

            foreach (KeyValuePair<string, JsonNode?> item in map)
            {
                if (item.Value is not null)
                {
                    return item.Value;
                }
            }
        }

        return root["rateLimits"];
    }

    private static QuotaWindow? ParseWindow(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        int usedPercent = ClampPercent(ReadInt(node, "usedPercent") ?? 0);
        int remainingPercent = ClampPercent(100 - usedPercent);
        int? windowDurationMins = ReadInt(node, "windowDurationMins");
        long? resetsAtSeconds = ReadLong(node, "resetsAt");

        return new QuotaWindow(
            UsedPercent: usedPercent,
            RemainingPercent: remainingPercent,
            WindowDuration: windowDurationMins is null ? null : TimeSpan.FromMinutes(windowDurationMins.Value),
            ResetsAt: resetsAtSeconds is null ? null : DateTimeOffset.FromUnixTimeSeconds(resetsAtSeconds.Value).ToLocalTime());
    }

    private static string? ReadString(JsonNode node, string propertyName)
    {
        return node[propertyName]?.GetValue<string>();
    }

    private static int? ReadInt(JsonNode node, string propertyName)
    {
        JsonNode? value = node[propertyName];
        return value is null ? null : Convert.ToInt32(value.GetValue<double>());
    }

    private static long? ReadLong(JsonNode node, string propertyName)
    {
        JsonNode? value = node[propertyName];
        return value is null ? null : Convert.ToInt64(value.GetValue<double>());
    }

    private static int ClampPercent(int value)
    {
        return Math.Max(0, Math.Min(100, value));
    }
}
