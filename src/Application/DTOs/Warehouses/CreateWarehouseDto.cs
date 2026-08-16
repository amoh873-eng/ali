using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Warehouses;

/// <summary>
/// DTO for creating a new warehouse.
/// </summary>
public class CreateWarehouseDto
{
    [Required(ErrorMessage = "كود المخزن مطلوب")]
    [StringLength(20, ErrorMessage = "الكود لا يتجاوز 20 حرفاً")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المخزن بالعربية مطلوب")]
    [StringLength(100, ErrorMessage = "الاسم لا يتجاوز 100 حرف")]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public bool IsDefault { get; set; }
}