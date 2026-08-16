using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Positions;

/// <summary>
/// DTO for updating an existing job position.
/// </summary>
public class UpdatePositionDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "كود المسمى الوظيفي مطلوب")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المسمى بالعربية مطلوب")]
    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    [Required(ErrorMessage = "القسم مطلوب")]
    public Guid DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
}
