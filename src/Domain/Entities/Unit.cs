using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a unit of measure (وحدة قياس) used by items.
/// Examples: قطعة، كيلو جرام، لتر، عبوة، متر.
/// Units are referenced by items to know how stock is counted.
/// </summary>
public class Unit : BaseEntity
{
    /// <summary>
    /// Unique unit code (e.g., "PCS", "KG", "L").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Unit name in Arabic (e.g., "قطعة").
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Unit name in English (e.g., "Piece").
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Whether this unit is active and selectable for new items.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system unit that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Items that use this unit as their base unit of measure.
    /// </summary>
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
