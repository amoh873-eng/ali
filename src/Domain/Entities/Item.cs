using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents an item (صنف) in the inventory.
/// An item belongs to one Category, is counted in one Unit,
/// and its stock level changes through StockMovement records.
/// </summary>
public class Item : BaseEntity
{
    /// <summary>
    /// Unique item code (e.g., "SKU-1001").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Item name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Item name in English.
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Foreign key to the category this item belongs to.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Navigation property to the category.
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Foreign key to the base unit of measure.
    /// </summary>
    public Guid UnitId { get; set; }

    /// <summary>
    /// Navigation property to the unit.
    /// </summary>
    public Unit? Unit { get; set; }

    /// <summary>
    /// Average cost price used to value inventory.
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Selling price shown on sales documents.
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Minimum stock level before a reorder is recommended.
    /// </summary>
    public decimal MinStockLevel { get; set; }

    /// <summary>
    /// Maximum stock level (for planning purposes).
    /// </summary>
    public decimal MaxStockLevel { get; set; }

    /// <summary>
    /// Optional barcode for scanning (SKU / EAN / UPC).
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this item is active and can be used in documents.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system item that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Current total stock across all warehouses.
    /// Denormalized cache — updated automatically by StockMovementService.
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// Concurrency token (SQL Server rowversion) — detects lost updates to stock
    /// when two writers change CurrentStock/CostPrice concurrently.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Stock movement history for this item.
    /// </summary>
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
