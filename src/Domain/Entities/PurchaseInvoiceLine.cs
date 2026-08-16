using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// بند في فاتورة مشتريات: صنف بكمية وتكلفة وحدة.
/// </summary>
public class PurchaseInvoiceLine : BaseEntity
{
    public Guid PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    /// <summary>الكمية المشتراة (موجبة دائماً).</summary>
    public decimal Quantity { get; set; }

    /// <summary>تكلفة الوحدة عند الشراء (تحديث المتوسط المرجح يعتمد عليها).</summary>
    public decimal UnitCost { get; set; }

    /// <summary>إجمالي البند = كمية × تكلفة الوحدة.</summary>
    public decimal LineTotal { get; set; }
}