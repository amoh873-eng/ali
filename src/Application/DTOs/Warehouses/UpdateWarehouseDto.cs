using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Warehouses;

/// <summary>
/// DTO for updating an existing warehouse.
/// </summary>
public class UpdateWarehouseDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "كود المخزن مطلوب")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المخزن بالعربية مطلوب")]
    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; }
}