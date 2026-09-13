using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// عهدة نقدية بيد مندوب المبيعات (RepCashCustody): مبلغ نقدي/شيك حُصِّل ميدانياً من عميل
/// (مرتبط بفاتورة أو دفعة على الحساب) ولم يُسلَّم للمكتب بعد.
///
/// الحساب المحاسبي حســاب واحد عام «عهدة نقدية بيد المندوبين» (أصل/مدين) والبُعد التحليلي
/// من المندوب عبر RepUserId — قرار وُكّد: تصفير العهدة عند التسليم بقيد محاسبي متوازن ذري
/// (نفس أسلوب CashDrawerTransaction):
///   - Collect : مدين «عهدة المندوبين» 1110 / دائن «ذمم العملاء» 1200
///   - Delivery: مدين «الصندوق» 1100 / دائن «عهدة المندوبين» 1110
/// </summary>
public class RepCashCustody
{
    public Guid Id { get; set; }

    /// <summary>التحصيل الميداني أم التسليم للمكتب.</summary>
    public RepCustodyEffect Effect { get; set; }

    /// <summary>معرّف المندوب (مفتاح إلى AspNetUsers.Id — البُعد التحليلي للعهدة).</summary>
    public string RepUserId { get; set; } = string.Empty;

    /// <summary>العميل الذي حُصِّل منه المبلغ (اختياري — مرجع تحليلي فقط).</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>الفاتورة المرتبطة بالتحصيل (اختياري — مرجع تحليلي فقط).</summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>أسلوب التحصيل: نقداً أو شيكاً (1 = نقدي / 2 = شيك كما في طرق السداد).</summary>
    public int PaymentMethod { get; set; } = 1;

    /// <summary>المبلغ (أكبر من صفر).</summary>
    public decimal Amount { get; set; }

    /// <summary>وقت العملية (محلي).</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>ملاحظة حرة اختيارية.</summary>
    public string? Notes { get; set; }

    /// <summary>معرّف القيد المحاسبي المرتبط (لتتبّع الأثر المحاسبي).</summary>
    public Guid JournalEntryId { get; set; }

    /// <summary>القيد المحاسبي المرتبط.</summary>
    public JournalEntry? JournalEntry { get; set; }

    public DateTime CreatedAt { get; set; }
}