using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a sales quote (عرض سعر) sent to a customer.
///
/// الفرق الجوهري بين "عرض السعر" و"الفاتورة":
/// عرض السعر مستند غير مُرحَّل (Non-Posting) — أي أنه لا يمسّ المخزون ولا الحسابات
/// إطلاقاً. إنما يثبّت الأسعار والكميات المقترحة للعميل. عند موافقة العميل يُحوَّل
/// العرض إلى فاتورة مبيعات فعلية، وهناك فقط تحدث حركات المخزون والقيود المحاسبية.
/// هذا الفصل يحمي النظام من تلويث المخزون بأسعار لم تُعتمد بعد.
/// </summary>
public class SalesQuote : BaseEntity
{
    /// <summary>
    /// Unique human-readable quote number (e.g., "SQ-20260201-0001").
    /// </summary>
    public string QuoteNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the customer receiving this quote.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the customer.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Date the quote was issued.
    /// </summary>
    public DateTime QuoteDate { get; set; }

    /// <summary>
    /// Optional expiry date after which the quote is no longer valid.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Lifecycle state (Draft / Approved / Converted / Cancelled).
    /// </summary>
    public QuoteStatus Status { get; set; }

    /// <summary>
    /// Sum of all line totals (before discount and tax).
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Header-level discount percentage (e.g., 5 = 5%).
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Computed discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Tax rate percentage applied on the discounted amount (e.g., 16 = 16%).
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Computed tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Final total = SubTotal − DiscountAmount + TaxAmount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Optional free-text note.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Id of the sales invoice created from this quote (set after conversion).
    /// Stored as a scalar Guid without a navigation property to keep the
    /// conversion a one-way link (quote → invoice), never the reverse.
    /// </summary>
    public Guid? ConvertedInvoiceId { get; set; }

    /// <summary>
    /// Line items of this quote.
    /// </summary>
    public ICollection<SalesQuoteLine> Lines { get; set; } = new List<SalesQuoteLine>();
}