using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a purchase order (أمر شراء) sent to a supplier.
///
/// مثل عرض السعر تماماً: أمر الشراء مستند "غير مُرحَّل" — لا يمسّ المخزون ولا الحسابات.
/// إنما يثبّت الكميات والأسعار المتفق عليها مع المورد. عند استلام البضاعة يُحوَّل إلى
/// فاتورة مشتريات فعلية، وهناك فقط تحدث حركة "وارد" وقيود المشتريات.
/// </summary>
public class PurchaseOrder : BaseEntity
{
    /// <summary>
    /// Unique human-readable order number (e.g., "PO-20260201-0001").
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the supplier receiving this order.
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Navigation property to the supplier.
    /// </summary>
    public Supplier? Supplier { get; set; }

    /// <summary>
    /// Date the order was issued.
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Optional expected delivery date.
    /// </summary>
    public DateTime? ExpectedDate { get; set; }

    /// <summary>
    /// Lifecycle state (Draft / Approved / Converted / Cancelled).
    /// </summary>
    public PurchaseOrderStatus Status { get; set; }

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
    /// Id of the purchase invoice created from this order (set after conversion).
    /// Scalar Guid without a navigation property (one-way link order → invoice).
    /// </summary>
    public Guid? ConvertedInvoiceId { get; set; }

    /// <summary>
    /// Line items of this order.
    /// </summary>
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}