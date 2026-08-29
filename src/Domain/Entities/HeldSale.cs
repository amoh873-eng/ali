namespace ERPSystem.Domain.Entities;

/// <summary>
/// عملية بيع موقوفة/معلّقة (Hold) من شاشة نقطة البيع.
/// يُحفظ هنا لقطة JSON كاملة للسلة الحالية (بلا تمثيل علائقي) لأن الميزة
/// قصيرة العمر وقليلة الحجم. لا تلمس المخزون أو الحسابات إطلاقاً — تتحوّل
/// إلى فاتورة حقيقية فقط عند استئنافها ثم إكمالها عبر SalesInvoiceService.
/// </summary>
public class HeldSale
{
    /// <summary>المعرّف الفريد للعملية الموقوفة.</summary>
    public Guid Id { get; set; }

    /// <summary>معرّف أمين الصندوق الذي أوقف العملية (مفتاح أجنبي إلى AspNetUsers.Id).</summary>
    public string CashierUserId { get; set; } = string.Empty;

    /// <summary>وقت إيقاف العملية.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>معرّف العميل المرتبط (اختياري — قد يكون زبون نقدي).</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>لقطة JSON للسلة: البنود (صنف، كمية، سعر وحدة، خصم للبند) + الخصم على الفاتورة.</summary>
    public string LinesJson { get; set; } = string.Empty;

    /// <summary>ملاحظة اختيارية يضيفها الكاشير (مثل: "أحمد - بانتظار المحفظة").</summary>
    public string? Note { get; set; }
}