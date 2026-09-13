using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.RepCustody;

/// <summary>نموذج إدخال عملية عهدة (تحصيل أو تسليم).</summary>
public class CreateRepCustodyDto
{
    /// <summary>التحصيل أم التسليم (يحدد اتجاه القيد المحاسبي).</summary>
    public RepCustodyEffect Effect { get; set; }

    /// <summary>معرّف المندوب (مطلوب للتحصيل والتسليم).</summary>
    public string RepUserId { get; set; } = string.Empty;

    /// <summary>العميل (اختياري).</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>الفاتورة المرتبطة (اختياري).</summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>أسلوب الدفع: 1 نقدي / 2 شيك.</summary>
    public int PaymentMethod { get; set; } = 1;

    /// <summary>المبلغ (أكبر من صفر).</summary>
    public decimal Amount { get; set; }

    /// <summary>ملاحظة.</summary>
    public string? Notes { get; set; }

    /// <summary>وقت العملية (افتراضياً الآن).</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>كائن عرض لعملية عهدة.</summary>
public class RepCustodyDto
{
    public Guid Id { get; set; }
    public RepCustodyEffect Effect { get; set; }
    public string EffectNameAr => Effect == RepCustodyEffect.Collect ? "تحصيل ميداني" : "تسليم للمكتب";
    public string RepUserId { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Guid? InvoiceId { get; set; }
    public int PaymentMethod { get; set; } = 1;
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
    public Guid JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = "—";
    public string EntryDescription { get; set; } = string.Empty;
}

/// <summary>رصيد عهدة مندوب حالي (مجموع التحصيل − مجموع التسليم).</summary>
public class RepCustodyBalanceDto
{
    public string RepUserId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

/// <summary>فلترة سجل العهود.</summary>
public class RepCustodyFilterDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Rep { get; set; }
}