using ERPSystem.Application.Services;

namespace ERPSystem.Tests;

/// <summary>
/// Unit tests for the weighted-average costing logic (تقييم المخزون).
/// </summary>
public class InventoryValuationTests
{
    [Fact]
    public void ComputeWeightedAverage_BasicCase_ReturnsRoundedAverage()
    {
        // (10 × 5 + 10 × 10) / 20 = 7.5
        var result = InventoryValuation.ComputeWeightedAverage(10m, 5m, 10m, 10m);
        Assert.Equal(7.5m, result);
    }

    [Fact]
    public void ComputeWeightedAverage_OpeningStock_UsesReceivedCost()
    {
        // oldQuantity = 0 → new cost equals the received unit cost
        var result = InventoryValuation.ComputeWeightedAverage(0m, 0m, 5m, 12m);
        Assert.Equal(12m, result);
    }

    [Fact]
    public void ComputeWeightedAverage_ZeroOrNegativeTotalQuantity_ReturnsOldCost()
    {
        var result = InventoryValuation.ComputeWeightedAverage(10m, 5m, -20m, 8m);
        Assert.Equal(5m, result);
    }

    [Fact]
    public void ComputeWeightedAverage_RoundsToTwoDecimals()
    {
        // (3 × 10 + 1 × 11.111) / 4 = 41.111 / 4 = 10.27775 → 10.28
        var result = InventoryValuation.ComputeWeightedAverage(3m, 10m, 1m, 11.111m);
        Assert.Equal(10.28m, result);
    }
}
