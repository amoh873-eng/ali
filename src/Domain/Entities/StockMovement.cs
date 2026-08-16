using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a single stock movement (حركة مخزون) for one item in one warehouse.
///
/// ملاحظة مهمة حول الإشارة:
/// Quantity تُخزن بإشارة (موجبة = وارد / سالبة = صادر).
/// هذا يجعل حساب الأرصدة بسيطاً: Sum(Quantity) للمجموعة (صنف + مخزن) يعطي الرصيد.
/// خدمة StockMovementService هي المسؤولة عن تطبيق الإشارة تلقائياً حسب نوع الحركة.
/// </summary>
public class StockMovement : BaseEntity
{
    /// <summary>
    /// Foreign key to the item being moved.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Navigation property to the item.
    /// </summary>
    public Item? Item { get; set; }

    /// <summary>
    /// Foreign key to the warehouse where the movement occurred.
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Navigation property to the warehouse.
    /// </summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// Type of movement (receipt, issue, adjustment, transfer, opening).
    /// Determines the effect sign on stock.
    /// </summary>
    public MovementType MovementType { get; set; }

    /// <summary>
    /// Signed quantity: positive = inbound, negative = outbound.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit cost at the time of the movement (for inventory valuation).
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Optional reference document number (e.g., invoice / PO number).
    /// Used to link two transfer movements together.
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Free-text note about the movement.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Date when the movement occurred (business date, not created date).
    /// </summary>
    public DateTime MovementDate { get; set; }
}
