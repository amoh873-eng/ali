namespace ERPSystem.Domain.Enums;

/// <summary>
/// Lifecycle of a sales quote (عرض سعر).
/// </summary>
public enum QuoteStatus
{
    /// <summary>مسودة - Draft (قابل للتعديل)</summary>
    Draft = 1,

    /// <summary>معتمد - Approved (جاهز للتحويل إلى فاتورة)</summary>
    Approved = 2,

    /// <summary>محوّل لفاتورة - Converted to invoice</summary>
    Converted = 3,

    /// <summary>ملغي - Cancelled</summary>
    Cancelled = 4
}