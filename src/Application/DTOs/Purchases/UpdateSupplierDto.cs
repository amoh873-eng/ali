using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>DTO لتعديل مورد.</summary>
public class UpdateSupplierDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "كود المورد مطلوب")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المورد بالعربية مطلوب")]
    [StringLength(200)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameEn { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? TaxNumber { get; set; }

    [Range(0, 999999999999)]
    public decimal? CreditLimit { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}