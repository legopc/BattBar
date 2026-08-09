namespace BattBar.Core;

/// <summary>
/// Pure decision functions for tray-icon rendering.
/// Lifted from Tray/BatteryIcon so tests can run on Linux without WinForms.
/// </summary>
public static class BatteryIconPredicates
{
    /// <summary>
    /// True iff the visible bitmap of the icon would change between a and b.
    /// Identity and display-only fields (BayIndex, DeviceId) are excluded by design.
    /// </summary>
    public static bool ShouldRedraw(BatteryStatus a, BatteryStatus b)
    {
        if (a.IsPresent != b.IsPresent) return true;
        if (a.State != b.State) return true;
        if (a.ChargePercent != b.ChargePercent) return true;
        return false;
    }
}