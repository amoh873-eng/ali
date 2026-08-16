using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Units;

/// <summary>
/// DTO for creating a new unit of measure.
/// </summary>
public class CreateUnitDto
{
    [Required(ErrorMessage = "كود الوحدة مطلوب")]
    [StringLength(20, ErrorMessage = "الكود لا يتجاوز 20 حرفاً")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الوحدة بالعربية مطلوب")]
    [StringLength(100, ErrorMessage = "الاسم لا يتجاوز 100 حرف")]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }
}