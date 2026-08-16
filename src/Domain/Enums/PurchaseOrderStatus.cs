namespace ERPSystem.Domain.Enums;

/// <summary>
/// Lifecycle of a purchase order (أمر شراء).
/// </summary>
public enum PurchaseOrderStatus
{
    /// <summary>مسودة - Draft (قابل للتعديل)</summary>
    Draft = 1,

    /// <summary>معتمد - Approved (جاهز للتحويل إلى فاتورة مشتريات)</summary>
    Approved = 2,

    /// <summary>محوّل لفاتورة - Converted to purchase invoice</summary>
    Converted = 3,

    /// <summary>ملغي - Cancelled</summary>
    Cancelled = 4
}