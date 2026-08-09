using System.Globalization;
using System.Text;

namespace BattBar.Core;

public static class TooltipFormatter
{
    public static string Format(IReadOnlyList<BatteryStatus> statuses)
    {
        if (statuses.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var s in statuses.OrderBy(s => s.BayIndex))
        {
            sb.AppendLine(FormatLine(s));
        }
        sb.Append("Combined: ").Append(FormatCombinedPercent(statuses)).Append('%');
        return sb.ToString();
    }

    private static string FormatLine(BatteryStatus s)
    {
        if (!s.IsPresent || s.State == BatteryState.NotPresent)
            return $"Battery {s.BayIndex + 1}: not present";

        if (s.ChargePercent is null)
            return $"Battery {s.BayIndex + 1}: ?? \u2014 {StateName(s.State)}";

        var suffix = s.ChargePercent < 5 && s.State == BatteryState.Discharging
            ? "Critical"
            : StateName(s.State);

        return $"Battery {s.BayIndex + 1}: {s.ChargePercent!.Value.ToString(CultureInfo.InvariantCulture)}% \u2014 {suffix}";
    }

    private static string StateName(BatteryState s) => s switch
    {
        BatteryState.Discharging => "Discharging",
        BatteryState.Charging => "Charging",
        BatteryState.Full => "Full",
        BatteryState.NotPresent => "not present",
        BatteryState.Unknown => "Unknown",
        _ => "Unknown"
    };

    private static int FormatCombinedPercent(IReadOnlyList<BatteryStatus> statuses)
    {
        var presentWithPct = statuses.Where(s => s.IsPresent && s.ChargePercent is not null).ToList();
        if (presentWithPct.Count == 0) return 0;

        var allHavePositiveCapacity = presentWithPct.All(s => s.FullChargeCapacity is > 0);
        if (allHavePositiveCapacity)
        {
            long usedMwh = 0;
            long totalMwh = 0;
            foreach (var s in presentWithPct)
            {
                usedMwh += (long)(s.FullChargeCapacity!.Value * s.ChargePercent!.Value);
                totalMwh += s.FullChargeCapacity.Value;
            }
            return totalMwh == 0 ? 0 : (int)Math.Round(usedMwh / (double)totalMwh);
        }
        else
        {
            return (int)Math.Round(presentWithPct.Average(s => s.ChargePercent!.Value));
        }
    }
}