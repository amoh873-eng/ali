using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// مساعد دُفعات المخزون (ItemBatch) — يُستخدم فقط للأصناف التي فعّلت Item.TracksBatches.
/// يعمل داخل نفس معاملة/قفل تغيير المخزون الحالية (نفس انضباط StockAvailabilityHelper).
/// المعادلة الحاكمة: مجموع كمية دُفعات (صنف، مخزن) === مجموع حركات المخزون (صنف، مخزن).
/// </summary>
internal static class ItemBatchHelper
{
    /// <summary>
    /// إنشاء دُفعة جديدة عند استلام مخزون لصنف TracksBatches.
    /// إن وُجدت دُفعة بنفس (رقم الدُفعة + تاريخ الانتهاء + الصنف + المخزن) ندمج الكميات بدلاً من
    /// إنشاء صف جديد (القرار المعتمد: الدمج يمنع تفتيت صفوف لا معنى لها لنفس الشحنة).
    /// </summary>
    public static async Task CreateOnReceiveAsync(DbContext context,
        Guid itemId, Guid warehouseId, decimal quantity,
        DateOnly? expiryDate, string? batchNumber, DateTime receivedDate,
        Guid? purchaseInvoiceId)
    {
        if (expiryDate is null)
            throw new InvalidOperationException(
                "هذا الصنف يتتبّع الدُفعات — تاريخ الانتهاء مطلوب عند استلام المخزون.");

        // الدمج فقط عندما يكون رقم الدُفعة غير فارغ (رقم فارد فعلي من المورد)
        if (!string.IsNullOrWhiteSpace(batchNumber))
        {
            var existing = await context.Set<ItemBatch>()
                .Where(b => b.ItemId == itemId && b.WarehouseId == warehouseId
                            && b.BatchNumber == batchNumber.Trim()
                            && b.ExpiryDate == expiryDate)
                .FirstOrDefaultAsync();

            if (existing is not null)
            {
                existing.Quantity += quantity;
                return;
            }
        }

        context.Set<ItemBatch>().Add(new ItemBatch
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            WarehouseId = warehouseId,
            BatchNumber = batchNumber,
            ProductionDate = null,
            ExpiryDate = expiryDate,
            Quantity = quantity,
            ReceivedDate = receivedDate,
            PurchaseInvoiceId = purchaseInvoiceId,
            LastExpiryNotifiedAt = null
        });
    }

    /// <summary>
    /// تخصيص كمية للبيع بأسلوب FEFO (الأقرب انتهاءً أولاً) مع **منع بيع أي كمية من دُفعة منتهية**.
    /// سحب الكمية حصراً من دُفعات غير منتهية (ExpiryDate &gt;= اليوم) مرتبة الأقرب انتهاءً أولاً.
    /// </summary>
    public static async Task AllocateFefoAsync(DbContext context,
        Guid itemId, Guid warehouseId, decimal requested)
    {
        if (requested <= 0m) return;

        var today = DateOnly.FromDateTime(DateTime.Today);

        var batches = await context.Set<ItemBatch>()
            .Where(b => b.ItemId == itemId && b.WarehouseId == warehouseId && b.Quantity > 0m)
            .ToListAsync();

        // البيع المسموح فقط من الدُفعات غير المنتهية (ExpiryDate >= اليوم) — تنتهي نافذة انتهاء الصلاحية
        // أولاً ثم بلا تاريخ. الدُفعات المنتهية تُستثنى بصمت لأن الكمية المطلوبة لن تُسحب منها أبداً.
        var usable = batches.Where(b => b.ExpiryDate is null || b.ExpiryDate >= today).ToList();
        if (usable.Count == 0)
            throw new InvalidOperationException(
                $@"
لا توجد دُفعات صالحة غير منتهية لهذا الصنف. كل الدُفعات المتبقية منتهية ولا يمكن بيعها.");

        // ترتيب: الأقرب انتهاءً أولاً، ثم البدون تاريخ في النهاية
        var ordered = usable
            .OrderBy(b => b.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(b => b.ExpiryDate)
            .ToList();

        var total = ordered.Sum(b => b.Quantity);
        if (total < requested)
            throw new InvalidOperationException(
                $"رصيد الدُفعات الصالحة غير كافٍ للصنف. المتاح غير المنتهي: {total:N0}، المطلوب: {requested:N0}.");

        var remaining = requested;
        foreach (var batch in ordered)
        {
            if (remaining <= 0m) break;
            var take = Math.Min(batch.Quantity, remaining);
            batch.Quantity -= take;
            remaining -= take;
        }
    }

    /// <summary>الدفعات المنتهية فعلياً (قبل اليوم) للصنف/المخزن — للعرض التحذيري.</summary>
    public static async Task<int> CountExpiredAsync(DbContext context, Guid itemId, Guid warehouseId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await context.Set<ItemBatch>()
            .Where(b => b.ItemId == itemId && b.WarehouseId == warehouseId
                        && b.Quantity > 0m && b.ExpiryDate != null && b.ExpiryDate < today)
            .CountAsync();
    }
}