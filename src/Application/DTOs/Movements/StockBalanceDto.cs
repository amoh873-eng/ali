namespace ERPSystem.Application.DTOs.Movements;

/// <summary>
/// DTO for the current stock balance of an item in a specific warehouse.
/// Used by the inventory balance report.
/// </summary>
public class StockBalanceDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemNameAr { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseNameAr { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue => Quantity * UnitCost;
}