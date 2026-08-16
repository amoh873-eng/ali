using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Accounts;

/// <summary>
/// DTO for creating a new account.
/// Contains only the fields needed for creation with validation attributes.
/// </summary>
public class CreateAccountDto
{
    /// <summary>
    /// Account code. Must be unique across all accounts.
    /// </summary>
    [Required(ErrorMessage = "كود الحساب مطلوب")]
    [StringLength(20, ErrorMessage = "الكود لا يتجاوز 20 حرفاً")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Arabic name of the account.
    /// </summary>
    [Required(ErrorMessage = "اسم الحساب بالعربية مطلوب")]
    [StringLength(200, ErrorMessage = "الاسم لا يتجاوز 200 حرف")]
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// English name (optional).
    /// </summary>
    [StringLength(200)]
    public string? NameEn { get; set; }

    /// <summary>
    /// Account type: Asset=1, Liability=2, Equity=3, Revenue=4, Expense=5
    /// </summary>
    [Required(ErrorMessage = "نوع الحساب مطلوب")]
    [Range(1, 5, ErrorMessage = "نوع الحساب غير صالح")]
    public int AccountType { get; set; }

    /// <summary>
    /// Parent account ID (optional — null for root accounts).
    /// </summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
}
