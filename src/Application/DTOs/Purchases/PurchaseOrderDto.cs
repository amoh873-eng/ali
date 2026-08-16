namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>
/// DTO for displaying a purchase order header together with its lines and supplier names.
/// </summary>
public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
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
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}