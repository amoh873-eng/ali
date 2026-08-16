using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a warehouse / store (مخزن) where stock is held.
/// Each warehouse keeps its own quantities per item, derived from movements.
/// </summary>
public class Warehouse : BaseEntity
{
    /// <summary>
    /// Unique warehouse code (e.g., "MAIN", "W2").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Warehouse name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Warehouse name in English.
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Physical location description (e.g., "المقر الرئيسي - الطابق الأول").
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Whether this warehouse is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system warehouse that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Whether this is the default warehouse used in new documents.
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Movement history recorded against this warehouse.
    /// </summary>
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
