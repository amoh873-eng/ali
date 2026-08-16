using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a single returned line item on a sales return (بند في مردود مبيعات).
/// </summary>
public class SalesReturnLine : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent return document.
    /// </summary>
    public Guid SalesReturnId { get; set; }

    /// <summary>
    /// Navigation property to the parent return document.
    /// </summary>
    public SalesReturn? SalesReturn { get; set; }

    /// <summary>
    /// Foreign key to the item being returned.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Navigation property to the item.
    /// </summary>
    public Item? Item { get; set; }

    /// <summary>
    /// Returned quantity (always positive).
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price copied from the original invoice line (we refund at the original price).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Unit cost copied from the original invoice line (to reverse COGS exactly).
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Line total = Quantity × UnitPrice.
    /// </summary>
    public decimal LineTotal { get; set; }
}
