namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>DTO لبند (إدخال) في مردود مشتريات.</summary>
public class CreatePurchaseReturnLineDto
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}