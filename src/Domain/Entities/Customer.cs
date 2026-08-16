using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a customer (عميل) who buys goods from the company.
///
/// ملاحظة معمارية حول "هل العميل حساب محاسبي أم كيان مستقل؟"
/// في الأنظمة المبسطة يُجعل العميل حساباً فرعياً تحت "العملاء/المدينون".
/// لكننا فضّلنا كياناً مستقلاً (Customer) لإدارته ببياناته (هاتف، عنوان، حد ائتماني)
/// ثم ربطناه محاسبياً عبر حساب واحد (العملاء) تُسجَّل عليه كل حركاته تلقائياً.
/// في مرحلة لاحقة يمكن تفصيل حساب مستقل لكل عميل إذا تطلب النظام ذلك.
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>
    /// Unique customer code (e.g., "C-1001").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Customer name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Customer name in English (optional).
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Physical address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Tax / VAT registration number (الرقم الضريبي).
    /// </summary>
    public string? TaxNumber { get; set; }

    /// <summary>
    /// Optional credit limit. Null means unlimited.
    /// </summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Denormalized current balance owed by this customer (rights on the company).
    /// Positive = customer owes us (مدين), updated automatically when invoices
    /// and returns are posted. Stored for fast dashboard/statement queries.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Whether this customer is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system customer that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Free-text notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Sales invoices belonging to this customer.
    /// </summary>
    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();

    /// <summary>
    /// Sales returns belonging to this customer.
    /// </summary>
    public ICollection<SalesReturn> SalesReturns { get; set; } = new List<SalesReturn>();
}
