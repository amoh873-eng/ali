namespace ERPSystem.Application.DTOs.Movements;

/// <summary>
/// DTO for displaying a stock count header with its lines.
/// </summary>
public class StockCountDto
{
    public Guid Id { get; set; }
    public string StockCountNumber { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public int Status { get; set; }
    public string? Note { get; set; }
    public int LinesCount { get; set; }
    public decimal TotalDifference { get; set; }
    public List<StockCountLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO for displaying a single stock count line.
/// </summary>
public class StockCountLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemNameAr { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Difference { get; set; }
}