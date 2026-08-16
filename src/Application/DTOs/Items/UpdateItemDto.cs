using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Items;

/// <summary>
/// DTO for updating an existing inventory item.
/// </summary>
public class UpdateItemDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "كود الصنف مطلوب")]
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الصنف بالعربية مطلوب")]
    [StringLength(200)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameEn { get; set; }

    [Required(ErrorMessage = "الفئة مطلوبة")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "الوحدة مطلوبة")]
    public Guid UnitId { get; set; }

    [Range(0, 9999999999)]
    public decimal CostPrice { get; set; }

    [Range(0, 9999999999)]
    public decimal SalePrice { get; set; }

    [Range(0, 9999999999)]
    public decimal MinStockLevel { get; set; }

    [Range(0, 9999999999)]
    public decimal MaxStockLevel { get; set; }

    [StringLength(50)]
    public string? Barcode { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}