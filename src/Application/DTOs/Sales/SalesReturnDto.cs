namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for displaying a sales return with its lines and references.
/// </summary>
public class SalesReturnDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SalesInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public int Status { get; set; }
    public string StatusNameAr => Status switch { 1 => "مسودة", 2 => "مرحّلة", 3 => "ملغاة", _ => "غير معروف" };
    public decimal SubTotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public List<SalesReturnLineDto> Lines { get; set; } = new();
}
