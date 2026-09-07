using ERPSystem.Application.DTOs.Inventory;

namespace ERPSystem.Application.Interfaces;

/// <summary>قراءة دُفعات المخزون (ItemBatch) والتنبيهات والتحقق من التكامل.</summary>
public interface IItemBatchService
{
    /// <summary>كل الدُفعات ذات الكمية الموجبة، الأقرب انتهاءً أولاً.</summary>
    Task<List<ItemBatchDto>> GetBatchesAsync();

    /// <summary>الدُفعات التي تنتهي خلال windowDays يوماً (أو منتهية فعلاً)، الأقرب أولاً.</summary>
    Task<List<ItemBatchDto>> GetExpiringBatchesAsync(int windowDays);

    /// <summary>عدد الدُفعات القريبة من الانتهاء (للمؤشر في الشريط العلوي/قائمة انتهاء الصلاحية).</summary>
    Task<int> GetExpiringCountAsync(int windowDays);

    /// <summary>معرّفات الأصناف التي لها دُفعة قريبة الانتهاء (لتمييز أزرار POS).</summary>
    Task<HashSet<Guid>> GetItemsWithNearExpiryAsync(int windowDays);

    /// <summary>
    /// تحقق داخل المعاملة الحالية من أن مجموع كمية دُفعات (صنف، مخزن) يطابق مجموع حركات المخزون
    /// لنفس (صنف، مخزن) — يُستدعى في نقاط الاستلام/الصرف للأصناف TracksBatches.
    /// </summary>
    Task AssertBatchesConsistentAsync(Guid itemId, Guid warehouseId);
}