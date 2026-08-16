using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Categories;

/// <summary>
/// DTO for updating an existing item category.
/// </summary>
public class UpdateCategoryDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "كود الفئة مطلوب")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الفئة بالعربية مطلوب")]
    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    public Guid? ParentCategoryId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}