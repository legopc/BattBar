using BattBar.Core;

namespace BattBar.Tray;

/// <summary>
/// Internal registry that lets <see cref="BatteryIcon"/> read the latest full status
/// list (needed for tooltip rendering) without owning the list itself.
/// Populated by <see cref="TrayHost"/>.
/// </summary>
internal static class BatteryIconRegistry
{
    private static IReadOnlyList<BatteryStatus> _current = Array.Empty<BatteryStatus>();
    public static void Set(IReadOnlyList<BatteryStatus> statuses) => _current = statuses;
    public static IReadOnlyList<BatteryStatus> SnapshotStatuses() => _current;
    public static Action? RefreshCheckedAction { get; set; }
}