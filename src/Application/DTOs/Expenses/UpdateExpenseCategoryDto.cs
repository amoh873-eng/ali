using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Expenses;

/// <summary>
/// DTO for updating an existing expense category.
/// </summary>
public class UpdateExpenseCategoryDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
