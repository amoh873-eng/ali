using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a periodic stock count (جرد دوري) for a single warehouse.
/// يحفظ الأرصدة النظامية عند الجرد مقابل الكميات الفعلية المعدودة،
/// وعند الترحيل يولّد حركات تسوية (AdjustmentIn/Out) للفروقات.
/// </summary>
public class StockCount : BaseEntity
{
    /// <summary>
    /// Unique human-readable count number (e.g., "SC-20260201-0001").
    /// </summary>
    public string StockCountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the warehouse being counted.
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Navigation property to the warehouse.
    /// </summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// Date the physical count was performed.
    /// </summary>
    public DateTime CountDate { get; set; }

    /// <summary>
    /// Lifecycle state (Posted immediately in this version).
    /// </summary>
    public DocumentStatus Status { get; set; }

    /// <summary>
    /// Optional free-text note.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Line items of this count.
    /// </summary>
    public ICollection<StockCountLine> Lines { get; set; } = new List<StockCountLine>();
}