using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Units;

/// <summary>
/// DTO for updating an existing unit of measure.
/// </summary>
public class UpdateUnitDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "كود الوحدة مطلوب")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الوحدة بالعربية مطلوب")]
    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    public bool IsActive { get; set; } = true;
}