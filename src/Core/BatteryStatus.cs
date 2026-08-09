namespace BattBar.Core;

public enum BatteryState
{
    Discharging,
    Charging,
    Full,
    NotPresent,
    Unknown
}

public sealed record BatteryStatus(
    int BayIndex,
    string DeviceId,
    bool IsPresent,
    BatteryState State,
    int? ChargePercent,
    int? FullChargeCapacity,
    int? DesignCapacity,
    string? StateRawText
);