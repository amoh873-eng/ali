using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Accounts;

/// <summary>
/// DTO for updating an existing account.
/// Similar to CreateAccountDto but includes the Id and allows partial updates.
/// </summary>
public class UpdateAccountDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "كود الحساب مطلوب")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الحساب بالعربية مطلوب")]
    [StringLength(200)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameEn { get; set; }

    [Required(ErrorMessage = "نوع الحساب مطلوب")]
    [Range(1, 5)]
    public int AccountType { get; set; }

    public Guid? ParentAccountId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
