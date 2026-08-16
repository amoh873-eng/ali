using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Departments;

/// <summary>
/// DTO for updating an existing department.
/// </summary>
public class UpdateDepartmentDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "كود القسم مطلوب")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم القسم بالعربية مطلوب")]
    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
