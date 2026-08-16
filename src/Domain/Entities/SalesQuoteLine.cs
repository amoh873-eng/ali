using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a single line item on a sales quote (بند في عرض سعر).
/// Unlike an invoice line, it has no UnitCost — a quote is only about the
/// selling price, since stock/cost effects happen only at conversion time.
/// </summary>
public class SalesQuoteLine : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent quote.
    /// </summary>
    public Guid SalesQuoteId { get; set; }

    /// <summary>
    /// Navigation property to the parent quote.
    /// </summary>
    public SalesQuote? SalesQuote { get; set; }

    /// <summary>
    /// Foreign key to the item being offered.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Navigation property to the item.
    /// </summary>
    public Item? Item { get; set; }

    /// <summary>
    /// Quantity offered (always positive).
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit selling price offered to the customer.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Line total = Quantity × UnitPrice (computed, stored for speed).
    /// </summary>
    public decimal LineTotal { get; set; }
}