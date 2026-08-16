using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Categories;

/// <summary>
/// DTO for creating a new item category.
/// </summary>
public class CreateCategoryDto
{
    [Required(ErrorMessage = "كود الفئة مطلوب")]
    [StringLength(20, ErrorMessage = "الكود لا يتجاوز 20 حرفاً")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الفئة بالعربية مطلوب")]
    [StringLength(100, ErrorMessage = "الاسم لا يتجاوز 100 حرف")]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    public Guid? ParentCategoryId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}