namespace ERPSystem.Application.DTOs.Items;

/// <summary>
/// Data Transfer Object for displaying an inventory item.
/// </summary>
public class ItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryNameAr { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public string UnitNameAr { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public decimal CurrentStock { get; set; }

    public bool TracksExpiry { get; set; }

    /// <summary>مسار صورة الصنف (يُعرض في أزرار POS وبطاقة المادة). فارغ إن لا صورة.</summary>
    public string? ImageUrl { get; set; }
}