using System.Management;

namespace BattBar.Core;

/// <summary>
/// One read cycle: query root\WMI:BatteryStatus and BatteryFullChargedCapacity,
/// join rows by InstanceName, return per-battery statuses.
/// Per-row failures are caught; one bad row must never poison the whole read.
/// </summary>
public sealed class BatteryReader
{
    public IReadOnlyList<BatteryStatus> ReadAll()
    {
        var results = new List<BatteryStatus>();
        Diag.Log("ReadAll: querying root\\WMI:BatteryStatus + BatteryFullChargedCapacity");

        // Build a map: InstanceName -> FullChargedCapacity
        var fullByInstance = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var fullSearcher = new ManagementObjectSearcher("root\\WMI", "SELECT InstanceName, FullChargedCapacity FROM BatteryFullChargedCapacity");
            using var fullCol = fullSearcher.Get();
            foreach (ManagementObject mo in fullCol)
            {
                try
                {
                    var inst = mo["InstanceName"]?.ToString();
                    var fcc = ToUInt32(mo["FullChargedCapacity"]);
                    if (!string.IsNullOrEmpty(inst) && fcc > 0)
                        fullByInstance[inst] = fcc;
                }
                finally { mo.Dispose(); }
            }
            Diag.Log($"  Loaded {fullByInstance.Count} BatteryFullChargedCapacity rows");
        }
        catch (Exception ex)
        {
            Diag.Log($"  BatteryFullChargedCapacity query failed: {ex.GetType().Name}: {ex.Message}");
        }

        // Designed capacity: not exposed via WMI on this firmware (BatteryDesignCapacity class is missing).
        // v2 health feature (designed vs full) won't work; everything else still functions.

        // Main per-battery status query.
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM BatteryStatus");
            using var collection = searcher.Get();

            int idx = 0;
            foreach (ManagementObject mo in collection)
            {
                try
                {
                    var status = ReadRow(mo, idx, fullByInstance);
                    results.Add(status);
                    Diag.Log($"  Row {idx}: Instance={status.DeviceId} Pct={status.ChargePercent?.ToString() ?? "null"} State={status.State} FCC={status.FullChargeCapacity}");
                }
                catch (Exception ex)
                {
                    Diag.Log($"  Row {idx} exception: {ex.GetType().Name}: {ex.Message}");
                    results.Add(new BatteryStatus(idx, $"unknown:{idx}", true, BatteryState.Unknown, null, null, null, ex.Message));
                }
                finally
                {
                    mo.Dispose();
                }
                idx++;
            }
        }
        catch (Exception ex)
        {
            Diag.Log($"ReadAll: BatteryStatus query failed: {ex.GetType().Name}: {ex.Message}");
            throw;
        }

        Diag.Log($"ReadAll: returning {results.Count} results");
        return results;
    }

    private static BatteryStatus ReadRow(ManagementObject mo, int initialBayIndex,
        Dictionary<string, uint> fullByInstance)
    {
        string instance = mo["InstanceName"]?.ToString() ?? $"unknown:{initialBayIndex}";

        uint remaining = ToUInt32(mo["RemainingCapacity"]);
        uint fullCharged = fullByInstance.TryGetValue(instance, out var fcc) ? fcc : 0u;
        uint designed = 0; // BatteryDesignCapacity class not exposed on this firmware

        // State: derive from boolean flags. Older WMI versions had PowerState; newer
        // versions expose Charging/Discharging/Critical/PowerOnline booleans.
        bool charging = ToBool(mo["Charging"]);
        bool discharging = ToBool(mo["Discharging"]);
        bool critical = ToBool(mo["Critical"]);
        bool powerOnline = ToBool(mo["PowerOnline"]);

        // Some versions also expose PowerState as uint32 — try it for completeness.
        uint powerState = 0;
        try { powerState = ToUInt32(mo["PowerState"]); } catch { /* not present */ }

        BatteryState state;
        // "Full" heuristic: when RemainingCapacity == FullChargedCapacity, the battery is
        // definitively at 100% — no need to rely on PowerState flags (which are not exposed
        // on this WMI version).
        bool atFull = fullCharged > 0 && remaining >= fullCharged;

        if (powerState != 0)
        {
            state = DecodePowerState(powerState);
            // Override: if WMI says Discharging but we're at 100%, it's actually Full.
            if (state == BatteryState.Discharging && atFull)
                state = BatteryState.Full;
        }
        else
        {
            if (charging) state = BatteryState.Charging;
            else if (atFull) state = BatteryState.Full;
            else if (discharging) state = BatteryState.Discharging;
            else if (critical) state = BatteryState.Discharging; // critical is a discharging sub-state
            else state = BatteryState.Unknown;
        }

        // Critical override on state: if critically low charge and discharging, keep as Discharging
        // and let TooltipFormatter do the "Critical" suffix from < 5% threshold.

        bool present = remaining > 0 && fullCharged > 0;
        int? pct = present
            ? (int)Math.Round(remaining / (double)fullCharged * 100.0)
            : (int?)null;

        return new BatteryStatus(
            BayIndex: initialBayIndex,
            DeviceId: instance,
            IsPresent: present,
            State: state,
            ChargePercent: pct,
            FullChargeCapacity: (int?)fullCharged,
            DesignCapacity: (int?)designed,
            StateRawText: null);
    }

    private static uint ToUInt32(object? v)
    {
        if (v is null || v is DBNull) return 0;
        try { return Convert.ToUInt32(v); } catch { return 0; }
    }

    private static bool ToBool(object? v)
    {
        if (v is null || v is DBNull) return false;
        try { return Convert.ToBoolean(v); } catch { return false; }
    }

    private static BatteryState DecodePowerState(uint powerState)
    {
        const uint BATTERY_CHARGING = 0x00000004;
        const uint BATTERY_DISCHARGING = 0x00000002;
        const uint BATTERY_POWER_ON_LINE = 0x00000001;

        if ((powerState & BATTERY_CHARGING) != 0) return BatteryState.Charging;
        if ((powerState & BATTERY_POWER_ON_LINE) != 0 && (powerState & BATTERY_DISCHARGING) == 0) return BatteryState.Full;
        if ((powerState & BATTERY_DISCHARGING) != 0) return BatteryState.Discharging;
        return BatteryState.Unknown;
    }

    /// <summary>
    /// WMI doesn't expose a separate composite; combine in TooltipFormatter instead.
    /// </summary>
    public int? ReadCompositePercent() => null;

    /// <summary>No-op in the WMI world; kept for API compatibility.</summary>
    public void LogCompositeDivergenceIfAny(IReadOnlyList<BatteryStatus> children) { }
}