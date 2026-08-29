using ERPSystem.Application.DTOs.Pos;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// خدمة عمليات البيع الموقوفة (Hold) من نقطة البيع — سلة محلية فقط،
/// لا تأثير على المخزون/الحسابات حتى تُستأنف وتُكتمل كفاتورة عادية.
/// </summary>
public interface IHeldSaleService
{
    /// <summary>حفظ عملية موقوفة جديدة (إنشاء ثم مسح السلة الحالية من الواجهة).</summary>
    Task<HeldSaleDto> HoldAsync(SaveHeldSaleDto dto);

    /// <summary>عمليات موقوفة خاصة بكاشير معين (تنازلياً بوقت الإنشاء).</summary>
    Task<List<HeldSaleDto>> GetByCashierAsync(string cashierUserId);

    /// <summary>جلب عملية موقوفة بمعرّفها.</summary>
    Task<HeldSaleDto?> GetByIdAsync(Guid id);

    /// <summary>حذف عملية موقوفة (عند الاستئناف أو الإلغاء الكامل).</summary>
    Task<bool> DeleteAsync(Guid id);
}