using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// مساعد دفعات المخزون (تتبّع انتهاء الصلاحية) — يُستخدم فقط للأصناف التي
/// فعّلت TracksExpiry. لا يمسّ StockMovement/CurrentStock إطلاقاً؛ يعمل كطبقة
/// معلومة موازية تحتها. يُستَخدم داخل نفس معاملة/قفل تغيير المخزون الحالية
/// (نفس انضباط StockAvailabilityHelper — قفل صف الصنف يمنع كتابتين متزامنتين).
/// </summary>
internal static class StockBatchHelper
{
    /// <summary>
    /// إنشاء دفعة جديدة عند استلام مخزون لصنف يتتبّع الانتهاء.
    /// يُستدعى من PurchaseInvoiceService بجانب حركة الوارد المعتادة.
    /// </summary>
    public static async Task CreateOnReceiveAsync(DbContext context,
        Guid itemId, Guid warehouseId, decimal quantity,
        DateOnly? expiryDate, string? batchNumber, string? reference, DateTime receivedDate)
    {
        context.Set<StockBatch>().Add(new StockBatch
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            WarehouseId = warehouseId,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            Quantity = quantity,
            ReceivedDate = receivedDate,
            LastExpiryNotifiedAt = null
        });
    }

    /// <summary>
    /// تخصيص كمية للبيع بأسلوب FEFO (الأقرب انتهاءً أولاً) للصنف الذي يتتبّع
    /// الانتهاء: يخصم من أقدم دفعة تاريخ انتهاء أولاً ثم التالية. إذا لم تكفِ
    /// الدفعات فالتاريخ ينتهي بخطأ عربي واضح (يُرجع المعاملة).
    /// </summary>
    public static async Task AllocateFefoAsync(DbContext context,
        Guid itemId, Guid warehouseId, decimal requested)
    {
        if (requested <= 0m) return;

        var batches = await context.Set<StockBatch>()
            .Where(b => b.ItemId == itemId && b.WarehouseId == warehouseId && b.Quantity > 0m)
            .ToListAsync();

        // نرتب في الذاكرة: الأقرب انتهاءً أولاً، وبدون تاريخ في النهاية.
        var ordered = batches
            .OrderBy(b => b.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(b => b.ExpiryDate)
            .ToList();

        var total = ordered.Sum(b => b.Quantity);
        if (total < requested)
            throw new InvalidOperationException(
                $"رصيد الدفعات غير كافٍ للصنف. المتاح في الدفعات: {total:N0}، المطلوب: {requested:N0}.");

        var remaining = requested;
        foreach (var batch in ordered)
        {
            if (remaining <= 0m) break;
            var take = Math.Min(batch.Quantity, remaining);
            batch.Quantity -= take;
            remaining -= take;
        }
    }
}