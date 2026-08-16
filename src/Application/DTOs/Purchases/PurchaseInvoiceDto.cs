namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>DTO لعرض فاتورة مشتريات مع بنودها.</summary>
public class PurchaseInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public int InvoiceType { get; set; }
    public string InvoiceTypeNameAr => InvoiceType == 1 ? "نقدي" : "آجل";
    public int Status { get; set; }
    public string StatusNameAr => Status switch { 1 => "مسودة", 2 => "مرحّلة", 3 => "ملغاة", _ => "غير معروف" };
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Note { get; set; }
    public List<PurchaseInvoiceLineDto> Lines { get; set; } = new();
}