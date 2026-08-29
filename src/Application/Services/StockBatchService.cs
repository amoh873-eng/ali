using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>قراءة دفعات المخزون لعرضها في صفحة تتبّع انتهاء الصلاحية.</summary>
public class StockBatchService : IStockBatchService
{
    private readonly DbContext _context;

    public StockBatchService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<StockBatchDto>> GetBatchesAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var batches = await _context.Set<StockBatch>()
            .Where(b => b.Quantity > 0)
            .Include(b => b.Item)
            .Include(b => b.Warehouse)
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.ExpiryDate.HasValue ? 0 : 1)
            .ToListAsync();

        return batches.Select(b => new StockBatchDto
        {
            Id = b.Id,
            ItemId = b.ItemId,
            ItemCode = b.Item?.Code ?? "—",
            ItemNameAr = b.Item?.NameAr ?? "—",
            WarehouseId = b.WarehouseId,
            WarehouseName = b.Warehouse?.NameAr ?? "—",
            BatchNumber = b.BatchNumber,
            ExpiryDate = b.ExpiryDate,
            Quantity = b.Quantity,
            ReceivedDate = b.ReceivedDate,
            Status = ResolveStatus(b.ExpiryDate, today),
        }).ToList();
    }

    internal static string ResolveStatus(DateOnly? expiry, DateOnly today)
    {
        if (expiry is null) return "Fine";               // بدون تاريخ انتهاء = عادي
        if (expiry.Value < today) return "Expired";      // منتهية فعلياً
        if (expiry.Value <= today.AddDays(7)) return "Expiring"; // قرب الانتهاء (النافذة الافتراضية)
        return "Fine";
    }
}