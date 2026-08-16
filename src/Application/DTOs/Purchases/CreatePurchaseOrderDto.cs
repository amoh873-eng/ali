using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>
/// DTO for creating a purchase order (أمر شراء).
/// A purchase order is a non-posting document — no stock or accounting effect.
/// </summary>
public class CreatePurchaseOrderDto
{
    [Required(ErrorMessage = "المورد مطلوب")]
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Date the order is issued.
    /// </summary>
    public DateTime OrderDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Optional expected delivery date.
    /// </summary>
    public DateTime? ExpectedDate { get; set; }

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
    /// The line items of the order. Must contain at least one line.
    /// </summary>
    [MinLength(1, ErrorMessage = "يجب إضافة بند واحد على الأقل")]
    public List<CreatePurchaseOrderLineDto> Lines { get; set; } = new();
}