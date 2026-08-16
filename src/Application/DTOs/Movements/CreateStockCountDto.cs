using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Movements;

/// <summary>
/// DTO for creating (and immediately posting) a stock count.
/// </summary>
public class CreateStockCountDto
{
    [Required(ErrorMessage = "المخزن مطلوب")]
    public Guid WarehouseId { get; set; }

    public DateTime CountDate { get; set; } = DateTime.Today;

    [StringLength(1000)]
    public string? Note { get; set; }

    /// <summary>
    /// Counted quantities keyed by item (only items the user entered a count for).
    /// </summary>
    public List<StockCountLineInput> Lines { get; set; } = new();
}

/// <summary>
/// A single input line for the stock count: item and its physically counted quantity.
/// </summary>
public class StockCountLineInput
{
    public Guid ItemId { get; set; }
    public decimal CountedQuantity { get; set; }
}