using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a single line item on a purchase order (بند في أمر شراء).
/// </summary>
public class PurchaseOrderLine : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent order.
    /// </summary>
    public Guid PurchaseOrderId { get; set; }

    /// <summary>
    /// Navigation property to the parent order.
    /// </summary>
    public PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>
    /// Foreign key to the item being ordered.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Navigation property to the item.
    /// </summary>
    public Item? Item { get; set; }

    /// <summary>
    /// Quantity ordered (always positive).
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price agreed with the supplier (becomes UnitCost on conversion).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Line total = Quantity × UnitPrice (computed, stored for speed).
    /// </summary>
    public decimal LineTotal { get; set; }
}