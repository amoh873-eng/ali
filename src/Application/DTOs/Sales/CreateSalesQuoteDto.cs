using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for creating a sales quote (عرض سعر).
/// A quote is a non-posting document — it does not affect stock or accounting.
/// </summary>
public class CreateSalesQuoteDto
{
    [Required(ErrorMessage = "العميل مطلوب")]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Date the quote is issued.
    /// </summary>
    public DateTime QuoteDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Optional expiry date after which the quote is no longer valid.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Header-level discount percentage (e.g., 5 = 5%).
    /// </summary>
    [Range(0, 100, ErrorMessage = "نسبة الخصم بين 0 و 100")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Tax rate percentage on the discounted amount (e.g., 16 = 16%).
    /// </summary>
    [Range(0, 100, ErrorMessage = "نسبة الضريبة بين 0 و 100")]
    public decimal TaxRate { get; set; } = 16;

    [StringLength(1000)]
    public string? Note { get; set; }

    /// <summary>
    /// The line items of the quote. Must contain at least one line.
    /// </summary>
    [MinLength(1, ErrorMessage = "يجب إضافة بند واحد على الأقل")]
    public List<CreateSalesQuoteLineDto> Lines { get; set; } = new();
}