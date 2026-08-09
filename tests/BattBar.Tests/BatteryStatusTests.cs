using Xunit;
using BattBar.Core;

namespace BattBar.Tests;

public class BatteryStatusTests
{
    [Fact]
    public void Record_Equality_ComparesAllFields()
    {
        var a = new BatteryStatus(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 73, 50000, 60000, "Discharging");
        var b = new BatteryStatus(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 73, 50000, 60000, "Discharging");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Record_Equality_TreatsDifferentChargePercentAsInequal()
    {
        var a = new BatteryStatus(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 73, 50000, 60000, null);
        var b = new BatteryStatus(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 74, 50000, 60000, null);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Record_With_ProducesNewInstanceWithChangedField()
    {
        var orig = new BatteryStatus(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 73, 50000, 60000, null);
        var next = orig with { ChargePercent = 70 };
        Assert.Equal(70, next.ChargePercent);
        Assert.Equal(73, orig.ChargePercent);
    }

    [Fact]
    public void Enum_HasFiveValues()
    {
        var values = Enum.GetValues<BatteryState>();
        Assert.Equal(5, values.Length);
        Assert.Contains(BatteryState.Discharging, values);
        Assert.Contains(BatteryState.Charging, values);
        Assert.Contains(BatteryState.Full, values);
        Assert.Contains(BatteryState.NotPresent, values);
        Assert.Contains(BatteryState.Unknown, values);
    }
}