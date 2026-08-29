using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Items;

/// <summary>
/// DTO for creating a new inventory item.
/// </summary>
public class CreateItemDto
{
    [Required(ErrorMessage = "كود الصنف مطلوب")]
    [StringLength(30, ErrorMessage = "الكود لا يتجاوز 30 حرفاً")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الصنف بالعربية مطلوب")]
    [StringLength(200, ErrorMessage = "الاسم لا يتجاوز 200 حرف")]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameEn { get; set; }

    [Required(ErrorMessage = "الفئة مطلوبة")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "الوحدة مطلوبة")]
    public Guid UnitId { get; set; }

    [Range(0, 9999999999, ErrorMessage = "سعر التكلفة غير صالح")]
    public decimal CostPrice { get; set; }

    [Range(0, 9999999999, ErrorMessage = "سعر البيع غير صالح")]
    public decimal SalePrice { get; set; }

    [Range(0, 9999999999, ErrorMessage = "حد أدنى غير صالح")]
    public decimal MinStockLevel { get; set; }

    [Range(0, 9999999999, ErrorMessage = "حد أقصى غير صالح")]
    public decimal MaxStockLevel { get; set; }

    [StringLength(50)]
    public string? Barcode { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>هل يتتبّع الصلاحية عبر دفعات (اختياري — للأصناف القابلة للتلف)؟</summary>
    public bool TracksExpiry { get; set; }
}