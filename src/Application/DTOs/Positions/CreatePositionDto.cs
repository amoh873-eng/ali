using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Positions;

/// <summary>
/// DTO for creating a new job position.
/// </summary>
public class CreatePositionDto
{
    [Required(ErrorMessage = "كود المسمى الوظيفي مطلوب")]
    [StringLength(20, ErrorMessage = "الكود لا يتجاوز 20 حرفاً")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المسمى بالعربية مطلوب")]
    [StringLength(100, ErrorMessage = "الاسم لا يتجاوز 100 حرف")]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    [Required(ErrorMessage = "القسم مطلوب")]
    public Guid DepartmentId { get; set; }
}
