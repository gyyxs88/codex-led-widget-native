namespace CodexLedWidget.Core;

/// <summary>仪表强调色。<paramref name="R"/> 等为 0-255 分量。</summary>
public readonly record struct QuotaAccent(byte R, byte G, byte B);

public static class QuotaPalette
{
    private static readonly QuotaAccent Healthy = new(24, 182, 115);
    private static readonly QuotaAccent Caution = new(241, 183, 47);
    private static readonly QuotaAccent Critical = new(228, 72, 92);

    /// <summary>剩余越多越绿、越少越红：100% 绿，50% 黄，0% 红，中间线性过渡。</summary>
    public static QuotaAccent AccentFor(int remainingPercent)
    {
        int percent = Math.Clamp(remainingPercent, 0, 100);
        return percent >= 50
            ? Blend(Caution, Healthy, (percent - 50) / 50.0)
            : Blend(Critical, Caution, percent / 50.0);
    }

    private static QuotaAccent Blend(QuotaAccent from, QuotaAccent to, double amount)
    {
        return new QuotaAccent(
            Mix(from.R, to.R, amount),
            Mix(from.G, to.G, amount),
            Mix(from.B, to.B, amount));
    }

    private static byte Mix(byte from, byte to, double amount)
    {
        return (byte)Math.Clamp(Math.Round(from + ((to - from) * amount)), 0, 255);
    }
}
