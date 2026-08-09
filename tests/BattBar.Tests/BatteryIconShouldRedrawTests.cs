using Xunit;
using BattBar.Core;

namespace BattBar.Tests;

public class BatteryIconShouldRedrawTests
{
    private static BatteryStatus B(bool present, BatteryState state, int? pct)
        => new(0, "ACPI\\PNP0C0A:0", present, state, pct, null, null, null);

    [Fact]
    public void NoChange_ReturnsFalse()
    {
        var a = B(true, BatteryState.Discharging, 73);
        var b = B(true, BatteryState.Discharging, 73);
        Assert.False(BatteryIconPredicates.ShouldRedraw(a, b));
    }

    [Fact]
    public void PresenceChange_ReturnsTrue()
    {
        var a = B(true, BatteryState.Discharging, 73);
        var b = B(false, BatteryState.NotPresent, null);
        Assert.True(BatteryIconPredicates.ShouldRedraw(a, b));
    }

    [Fact]
    public void PercentChange_ReturnsTrue()
    {
        var a = B(true, BatteryState.Discharging, 73);
        var b = B(true, BatteryState.Discharging, 74);
        Assert.True(BatteryIconPredicates.ShouldRedraw(a, b));
    }

    [Fact]
    public void StateChange_ReturnsTrue()
    {
        var a = B(true, BatteryState.Discharging, 73);
        var b = B(true, BatteryState.Charging, 73);
        Assert.True(BatteryIconPredicates.ShouldRedraw(a, b));
    }

    [Fact]
    public void IrrelevantFieldChange_ReturnsFalse()
    {
        // DeviceId and BayIndex affect display/identity, but ShouldRedraw's contract
        // is "renderable visual change" — those fields don't change the bitmap.
        var a = B(true, BatteryState.Discharging, 73);
        var b = a with { DeviceId = "ACPI\\PNP0C0A:1" };
        var c = a with { BayIndex = 9 };
        var d = a with { StateRawText = "anything" };
        Assert.False(BatteryIconPredicates.ShouldRedraw(a, b));
        Assert.False(BatteryIconPredicates.ShouldRedraw(a, c));
        Assert.False(BatteryIconPredicates.ShouldRedraw(a, d));
    }
}