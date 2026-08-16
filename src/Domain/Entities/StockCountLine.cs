using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a single line in a stock count (بند في جرد دوري):
/// the item, the system's recorded quantity, the physically counted quantity,
/// and the difference (counted − system).
/// </summary>
public class StockCountLine : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent count.
    /// </summary>
    public Guid StockCountId { get; set; }

    /// <summary>
    /// Navigation property to the parent count.
    /// </summary>
    public StockCount? StockCount { get; set; }

    /// <summary>
    /// Foreign key to the counted item.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Navigation property to the item.
    /// </summary>
    public Item? Item { get; set; }

    /// <summary>
    /// The quantity recorded by the system at count time.
    /// </summary>
    public decimal SystemQuantity { get; set; }

    /// <summary>
    /// The physically counted quantity entered by the user.
    /// </summary>
    public decimal CountedQuantity { get; set; }

    /// <summary>
    /// Difference = CountedQuantity − SystemQuantity (positive = surplus, negative = shortage).
    /// </summary>
    public decimal Difference { get; set; }
}