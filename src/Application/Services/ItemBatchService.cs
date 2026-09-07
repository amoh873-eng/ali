using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>قراءة دُفعات المخزون (ItemBatch) للعرض والتنبيهات والتكامل مع CurrentStock/حركات المخزون.</summary>
public class ItemBatchService : IItemBatchService
{
    private readonly DbContext _context;

    public ItemBatchService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<ItemBatchDto>> GetBatchesAsync()
    {
        var batches = await _context.Set<ItemBatch>()
            .Where(b => b.Quantity > 0m)
            .Include(b => b.Item)
            .Include(b => b.Warehouse)
            .Include(b => b.PurchaseInvoice)
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.ExpiryDate.HasValue ? 0 : 1)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        return batches.Select(b => Map(b, today)).ToList();
    }

    public async Task<List<ItemBatchDto>> GetExpiringBatchesAsync(int windowDays)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(Math.Max(1, windowDays));

        var batches = await _context.Set<ItemBatch>()
            .Where(b => b.Quantity > 0m && b.ExpiryDate != null && b.ExpiryDate <= cutoff)
            .Include(b => b.Item)
            .Include(b => b.Warehouse)
            .Include(b => b.PurchaseInvoice)
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.ExpiryDate.HasValue ? 0 : 1)
            .ToListAsync();

        return batches.Select(b => Map(b, today)).ToList();
    }

    public async Task<int> GetExpiringCountAsync(int windowDays)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(Math.Max(1, windowDays));

        return await _context.Set<ItemBatch>()
            .Where(b => b.Quantity > 0m && b.ExpiryDate != null && b.ExpiryDate <= cutoff)
            .CountAsync();
    }

    public async Task<HashSet<Guid>> GetItemsWithNearExpiryAsync(int windowDays)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(Math.Max(1, windowDays));

        var rows = await _context.Set<ItemBatch>()
            .Where(b => b.Quantity > 0m && b.ExpiryDate != null && b.ExpiryDate <= cutoff)
            .Select(b => b.ItemId)
            .Distinct()
            .ToListAsync();

        return rows.ToHashSet();
    }

    /// <summary>
    /// يتحقق أن مجموع كميات دُفعات (صنف، مخزن) == مجموع حركات المخزون لنفس (صنف، مخزن).
    /// يُستدعى داخل نفس معاملة الاستلام/الصرف. لو تعارض → استثناء واضح يتراجع عن المعاملة.
    /// </summary>
    public async Task AssertBatchesConsistentAsync(Guid itemId, Guid warehouseId)
    {
        var batchTotal = await _context.Set<ItemBatch>()
            .Where(b => b.ItemId == itemId && b.WarehouseId == warehouseId)
            .SumAsync(b => (decimal?)b.Quantity) ?? 0m;

        var movementTotal = await _context.Set<StockMovement>()
            .Where(m => m.ItemId == itemId && m.WarehouseId == warehouseId)
            .SumAsync(m => (decimal?)m.Quantity) ?? 0m;

        if (batchTotal != movementTotal)
            throw new InvalidOperationException(
                $"تعارض تكامل دُفعات: مجموع الدُفعات ({batchTotal:N2}) لا يطابق رصيد حركات المخزون ({movementTotal:N2}) للصنف/المخزن.");
    }

    private static ItemBatchDto Map(ItemBatch b, DateOnly today) => new()
    {
        Id = b.Id,
        ItemId = b.ItemId,
        ItemCode = b.Item?.Code ?? "—",
        ItemNameAr = b.Item?.NameAr ?? "—",
        WarehouseId = b.WarehouseId,
        WarehouseName = b.Warehouse?.NameAr ?? "—",
        BatchNumber = b.BatchNumber,
        ProductionDate = b.ProductionDate,
        ExpiryDate = b.ExpiryDate,
        Quantity = b.Quantity,
        ReceivedDate = b.ReceivedDate,
        PurchaseInvoiceNumber = b.PurchaseInvoice?.InvoiceNumber,
        Status = ResolveStatus(b.ExpiryDate, today)
    };

    internal static string ResolveStatus(DateOnly? expiry, DateOnly today)
    {
        if (expiry is null) return "Fine";
        if (expiry.Value < today) return "Expired";
        if (expiry.Value <= today.AddDays(7)) return "Expiring";
        return "Fine";
    }
}