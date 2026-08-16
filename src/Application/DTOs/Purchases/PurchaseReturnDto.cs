namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>DTO لعرض مردود مشتريات.</summary>
public class PurchaseReturnDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid PurchaseInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public int Status { get; set; }
    public string StatusNameAr => Status switch { 1 => "مسودة", 2 => "مرحّلة", 3 => "ملغاة", _ => "غير معروف" };
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public List<PurchaseReturnLineDto> Lines { get; set; } = new();
}