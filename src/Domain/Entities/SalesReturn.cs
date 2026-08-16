using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a sales return / refund (مردود مبيعات) linked to an original invoice.
/// When posted it:
/// 1. Returns the goods to stock (يمرر حركة مخزون واردة),
/// 2. Reverses the sale journal entry (يعكس قيد البيع),
/// 3. Reverses the COGS entry (يعكس قيد التكلفة).
/// يستخدم نوع حركة مخزون "وارد مردود" (SalesReturnIn) لإعادة البضاعة للمخزن.
/// </summary>
public class SalesReturn : BaseEntity
{
    /// <summary>
    /// Unique human-readable return number (e.g., "SR-20260201-0001").
    /// </summary>
    public string ReturnNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the original invoice being returned.
    /// </summary>
    public Guid SalesInvoiceId { get; set; }

    /// <summary>
    /// Navigation property to the original invoice.
    /// </summary>
    public SalesInvoice? SalesInvoice { get; set; }

    /// <summary>
    /// Foreign key to the customer (same as invoice customer, denormalized for fast queries).
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the customer.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Warehouse that receives the returned goods back into stock.
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Navigation property to the warehouse receiving the returned goods.
    /// </summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// Business date of the return.
    /// </summary>
    public DateTime ReturnDate { get; set; }

    /// <summary>
    /// Lifecycle status.
    /// </summary>
    public DocumentStatus Status { get; set; }

    /// <summary>
    /// Sum of all returned line totals.
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Discount percentage copied from the original invoice.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Computed discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Tax rate copied from the original invoice.
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Computed tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total refunded = SubTotal − DiscountAmount + TaxAmount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Note about the reason for the return.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Returned line items.
    /// </summary>
    public ICollection<SalesReturnLine> Lines { get; set; } = new List<SalesReturnLine>();

    /// <summary>
    /// The journal entry that reverses the sale (recorded on posting).
    /// </summary>
    public Guid? SalesJournalEntryId { get; set; }

    /// <summary>
    /// The journal entry that reverses the COGS (recorded on posting).
    /// </summary>
    public Guid? CogsJournalEntryId { get; set; }
}
