namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for displaying a sales invoice header together with its lines and customer/warehouse names.
/// </summary>
public class SalesInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public int InvoiceType { get; set; }
    public string InvoiceTypeNameAr => InvoiceType == 1 ? "نقدي" : "آجل";
    public int Status { get; set; }
    public string StatusNameAr => Status switch { 1 => "مسودة", 2 => "مرحّلة", 3 => "ملغاة", _ => "غير معروف" };
    public decimal SubTotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Note { get; set; }
    public List<SalesInvoiceLineDto> Lines { get; set; } = new();
    public Guid? SalesJournalEntryId { get; set; }
    public Guid? CogsJournalEntryId { get; set; }
    public bool IsPos { get; set; }
    public string? JoFotaraReferenceNumber { get; set; }
    public string? JoFotaraQrCode { get; set; }
    public DateTime? JoFotaraSubmittedAt { get; set; }
    public int JoFotaraStatus { get; set; }
}
