namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for displaying a sales quote header together with its lines and customer names.
/// </summary>
public class SalesQuoteDto
{
    public Guid Id { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public DateTime QuoteDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int Status { get; set; }
    public string StatusNameAr => Status switch { 1 => "مسودة", 2 => "معتمد", 3 => "محوّل لفاتورة", 4 => "ملغي", _ => "غير معروف" };
    public decimal SubTotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public Guid? ConvertedInvoiceId { get; set; }
    public List<SalesQuoteLineDto> Lines { get; set; } = new();
}