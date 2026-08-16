namespace ERPSystem.Application.DTOs.Movements;

/// <summary>
/// Data Transfer Object for displaying a stock movement in history tables.
/// </summary>
public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemNameAr { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseNameAr { get; set; } = string.Empty;
    public int MovementType { get; set; }
    public string MovementTypeNameAr { get; set; } = string.Empty;

    /// <summary>
    /// Signed quantity: positive = inbound, negative = outbound.
    /// </summary>
    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }
    public decimal Total => Quantity * UnitCost;
    public string? ReferenceNumber { get; set; }
    public string? Note { get; set; }
    public DateTime MovementDate { get; set; }
}