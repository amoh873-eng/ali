using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for creating (and immediately posting) a sales invoice.
/// </summary>
public class CreateSalesInvoiceDto
{
    [Required(ErrorMessage = "العميل مطلوب")]
    public Guid CustomerId { get; set; }

    [Required(ErrorMessage = "المخزن مطلوب")]
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Business date of the invoice.
    /// </summary>
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// 1 = نقدي (Cash), 2 = آجل (On-Account).
    /// </summary>
    [Required(ErrorMessage = "نوع الفاتورة مطلوب")]
    [Range(1, 2, ErrorMessage = "نوع الفاتورة غير صالح")]
    public int InvoiceType { get; set; }

    /// <summary>
    /// Header-level discount percentage (e.g., 5 = 5%).
    /// </summary>
    [Range(0, 100, ErrorMessage = "نسبة الخصم بين 0 و 100")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Tax rate percentage on the discounted amount (e.g., 16 = 16% for Jordan GST).
    /// </summary>
    [Range(0, 100, ErrorMessage = "نسبة الضريبة بين 0 و 100")]
    public decimal TaxRate { get; set; } = 16;

    [StringLength(1000)]
    public string? Note { get; set; }

    /// <summary>
    /// Marks this invoice as issued from the Point of Sale (POS) screen.
    /// </summary>
    public bool IsPos { get; set; }

    /// <summary>
    /// The line items of the invoice. Must contain at least one line.
    /// </summary>
    [MinLength(1, ErrorMessage = "يجب إضافة بند واحد على الأقل")]
    public List<CreateSalesInvoiceLineDto> Lines { get; set; } = new();
}
