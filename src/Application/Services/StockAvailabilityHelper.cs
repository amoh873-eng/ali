using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Shared stock-availability guard used by every service that creates an outbound stock movement.
/// Kept in a single place so the check cannot drift between call sites.
/// </summary>
internal static class StockAvailabilityHelper
{
    /// <summary>
    /// Ensures the available stock (sum of movements for the item in the warehouse) is enough for the requested quantity.
    /// </summary>
    public static async Task EnsureEnoughStockAsync(DbContext context, Guid itemId, Guid warehouseId, decimal quantity)
    {
        var available = await context.Set<StockMovement>()
            .Where(m => m.ItemId == itemId && m.WarehouseId == warehouseId)
            .SumAsync(m => (decimal?)m.Quantity) ?? 0m;

        if (available < quantity)
            throw new InvalidOperationException(
                $"الرصيد غير كافٍ للصنف. المتاح: {available:N0}، المطلوب: {quantity:N0}.");
    }
}
