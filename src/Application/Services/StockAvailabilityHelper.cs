using System.Data;
using ERPSystem.Application.Exceptions;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
        // Serialize concurrent stock mutations for the same item (lock held until the ambient transaction commits).
        await LockItemRowAsync(context, itemId);

        var available = await context.Set<StockMovement>()
            .Where(m => m.ItemId == itemId && m.WarehouseId == warehouseId)
            .SumAsync(m => (decimal?)m.Quantity) ?? 0m;

        if (available < quantity)
        {
            // جلب اسم الصنف ليظهر في الرسالة ويعرف المستخدم أي صنف يعيق العملية،
            // مع تمرير المعرّف ليتمكن واجهة المستخدم من تمييز هذا الصنف بصرياً.
            var item = await context.Set<Item>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == itemId);
            var itemName = item?.NameAr ?? item?.NameEn ?? itemId.ToString("N");

            throw new InsufficientStockException(itemId, itemName, available, quantity);
        }
    }

    private static async Task LockItemRowAsync(DbContext context, Guid itemId)
    {
        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "SELECT 1 FROM \"Items\" WHERE \"Id\" = @itemId FOR UPDATE";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "itemId";
            parameter.Value = itemId;
            command.Parameters.Add(parameter);

            await command.ExecuteScalarAsync();
        }
        finally
        {
            if (wasClosed && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }
}
