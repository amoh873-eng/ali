using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// وردية كاشير تُغلَق بتسوية (Shift Reconciliation): الرصيد المتوقَّع في درج الكاش
/// = الفكة الافتتاحية + Σ المبيعات النقدية − Σ المردودات النقدية + Σ الإضافات − Σ السحوبات
/// (كلها ضمن النطاق الزمني [StartTime, EndTime) وللكاشير نفسه تحديداً).
/// عند الإغلاق يُدخل الكاشير المبلغ المعدود فعلياً، والفرق عن المتوقَّع يُرحَّل قيداً
/// على «فروقات الصندوق» (5110) مقابل «نقدية - درج الكاش» (1105) إن لم يكن صفراً.
/// </summary>
public class CashierShift
{
    public Guid Id { get; set; }

    /// <summary>الكاشير صاحب الوردية (مفتاح إلى AspNetUsers.Id).</summary>
    public string CashierUserId { get; set; } = string.Empty;

    /// <summary>وقت فتح الوردية.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>وقت الإغلاق (فارغ أثناء الوردية المفتوحة).</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>مفتوحة أم مغلقة.</summary>
    public CashierShiftStatus Status { get; set; } = CashierShiftStatus.Open;

    /// <summary>رصيد الفكة الافتتاحي (يُربط بمعاملة Float-In التلقائية في OpeningTransferId).</summary>
    public decimal OpeningFloatAmount { get; set; }

    /// <summary>معرّف معاملة Float-In المرتبطة ببدء الوردية (التمويل من الخزنة إلى الدرج).</summary>
    public Guid OpeningTransferId { get; set; }

    /// <summary>الرصيد المتوقَّع المحسوب تلقائياً عند الإغلاق (المعادلة أعلاه).</summary>
    public decimal? ExpectedCashAmount { get; set; }

    /// <summary>المبلغ المعدود فعلياً — يدخل يدوياً عند الإغلاق.</summary>
    public decimal? CountedCashAmount { get; set; }

    /// <summary>الفرق = المعدود − المتوقَّع (موجب زيادة / سالب عجز).</summary>
    public decimal? VarianceAmount { get; set; }

    /// <summary>من أغلق الوردية (مفتاح إلى AspNetUsers.Id).</summary>
    public string? ClosedByUserId { get; set; }

    /// <summary>ملاحظة حرة اختيارية.</summary>
    public string? Notes { get; set; }

    /// <summary>قيد «فروقات الصندوق» إن وُجد فرق (وإلا بلا قيد).</summary>
    public Guid? JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}