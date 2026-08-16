using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Departments;

/// <summary>
/// DTO for creating a new department.
/// </summary>
public class CreateDepartmentDto
{
    [Required(ErrorMessage = "كود القسم مطلوب")]
    [StringLength(20, ErrorMessage = "الكود لا يتجاوز 20 حرفاً")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم القسم بالعربية مطلوب")]
    [StringLength(100, ErrorMessage = "الاسم لا يتجاوز 100 حرف")]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
