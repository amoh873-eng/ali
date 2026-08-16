using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Customers;

/// <summary>
/// DTO for creating a new customer.
/// </summary>
public class CreateCustomerDto
{
    [Required(ErrorMessage = "كود العميل مطلوب")]
    [StringLength(20, ErrorMessage = "الكود لا يتجاوز 20 حرفاً")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم العميل بالعربية مطلوب")]
    [StringLength(200)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameEn { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? TaxNumber { get; set; }

    [Range(0, 999999999999, ErrorMessage = "الحد الائتماني غير صالح")]
    public decimal? CreditLimit { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
