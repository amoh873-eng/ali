namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>DTO لبند (إدخال) في فاتورة مشتريات.</summary>
public class CreatePurchaseInvoiceLineDto
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}