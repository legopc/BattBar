using Xunit;
using BattBar.Core;

namespace BattBar.Tests;

public class TooltipFormatterTests
{
    private static BatteryStatus B(int idx, string id, bool present, BatteryState state, int? pct, int? fcc = null)
        => new(idx, id, present, state, pct, fcc, null, null);

    [Fact]
    public void Format_TwoBatteriesDischarging_ProducesThreeLines()
    {
        var statuses = new[]
        {
            B(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 73),
            B(1, "ACPI\\PNP0C0A:1", true, BatteryState.Discharging, 41),
        };
        var result = TooltipFormatter.Format(statuses);
        var lines = result.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("Battery 1: 73% \u2014 Discharging", lines[0]);
        Assert.Equal("Battery 2: 41% \u2014 Discharging", lines[1]);
        Assert.Equal("Combined: 57%", lines[2]);
    }

    [Fact]
    public void Format_OnePresentOneEmpty_RendersEmptyBayAsNotPresent()
    {
        var statuses = new[]
        {
            B(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 73, 50000),
            B(1, "ACPI\\PNP0C0A:1", false, BatteryState.NotPresent, null, null),
        };
        var result = TooltipFormatter.Format(statuses);
        Assert.Contains("Battery 1: 73% \u2014 Discharging", result);
        Assert.Contains("Battery 2: not present", result);
    }

    [Fact]
    public void Format_NullPercent_RendersDoubleQuestionMark()
    {
        var statuses = new[]
        {
            B(0, "ACPI\\PNP0C0A:0", true, BatteryState.Unknown, null, null),
        };
        var result = TooltipFormatter.Format(statuses);
        Assert.Contains("Battery 1: ?? \u2014 Unknown", result);
    }

    [Fact]
    public void Format_WeightedAverageUsedWhenCapacitiesKnown()
    {
        // Battery 1: 80%, 70Wh -> contributes 56 Wh
        // Battery 2: 20%, 30Wh -> contributes 6 Wh
        // Total capacity 100 Wh, used 62 -> 62%
        var statuses = new[]
        {
            B(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 80, 70000),
            B(1, "ACPI\\PNP0C0A:1", true, BatteryState.Discharging, 20, 30000),
        };
        var result = TooltipFormatter.Format(statuses);
        Assert.Contains("Combined: 62%", result);
    }

    [Fact]
    public void Format_SimpleAverageWhenCapacitiesMissingOrZero()
    {
        var statuses = new[]
        {
            B(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 80, 0),
            B(1, "ACPI\\PNP0C0A:1", true, BatteryState.Discharging, 20, 30000),
        };
        // When not all FCC are non-zero, fall back to simple average of present batteries' percentages.
        // Simple avg of 80 and 20 is 50.
        var result = TooltipFormatter.Format(statuses);
        Assert.Contains("Combined: 50%", result);
    }

    [Fact]
    public void Format_NotPresentAndUnknownAreExcludedFromCombined()
    {
        // Only Battery 1 should be counted toward combined.
        var statuses = new[]
        {
            B(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 80, 50000),
            B(1, "ACPI\\PNP0C0A:1", false, BatteryState.NotPresent, null, null),
        };
        var result = TooltipFormatter.Format(statuses);
        Assert.Contains("Combined: 80%", result);
    }

    [Fact]
    public void Format_BelowFivePercent_ShowsCritical()
    {
        var statuses = new[]
        {
            B(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 3, 50000),
        };
        var result = TooltipFormatter.Format(statuses);
        Assert.Contains("Battery 1: 3% \u2014 Critical", result);
    }

    [Fact]
    public void Format_ChargingState_RendersCharging()
    {
        var statuses = new[]
        {
            B(0, "ACPI\\PNP0C0A:0", true, BatteryState.Charging, 73, 50000),
        };
        var result = TooltipFormatter.Format(statuses);
        Assert.Contains("Charging", result);
    }

    [Fact]
    public void Format_ThreeBatteries_ListsAllThree()
    {
        // Generic robustness — should work even though real hardware caps at 2.
        var statuses = new[]
        {
            B(0, "ACPI\\PNP0C0A:0", true, BatteryState.Discharging, 50, 30000),
            B(1, "ACPI\\PNP0C0A:1", true, BatteryState.Discharging, 60, 30000),
            B(2, "ACPI\\PNP0C0A:2", true, BatteryState.Discharging, 70, 30000),
        };
        var result = TooltipFormatter.Format(statuses);
        Assert.Contains("Battery 1:", result);
        Assert.Contains("Battery 2:", result);
        Assert.Contains("Battery 3:", result);
    }

    [Fact]
    public void Format_NoBatteries_ReturnsEmptyString()
    {
        var result = TooltipFormatter.Format(Array.Empty<BatteryStatus>());
        Assert.Equal(string.Empty, result);
    }
}