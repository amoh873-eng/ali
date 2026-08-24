using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// فاتورة مشتريات (Purchase Invoice) من مورد.
/// المحاسبة: مدين (مخزون البضاعة) ، دائن (الصندوق نقدي / الموردين آجل) بإجمالي الفاتورة.
/// </summary>
public class PurchaseInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DateTime InvoiceDate { get; set; }
    public PurchaseInvoiceType InvoiceType { get; set; }
    public DocumentStatus Status { get; set; }

    /// <summary>مجموع بنود الفاتورة (كمية × تكلفة الوحدة).</summary>
    public decimal SubTotal { get; set; }

    /// <summary>الإجمالي النهائي (= SubTotal في المرحلة الأولى دون خصم/ضريبة).</summary>
    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TaxAmount { get; set; }
    public string? Note { get; set; }

    public ICollection<PurchaseInvoiceLine> Lines { get; set; } = new List<PurchaseInvoiceLine>();
    public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();

    /// <summary>القيد المحاسبي الناتج عند الترحيل.</summary>
    public Guid? JournalEntryId { get; set; }
}