namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>
/// DTO for a single line item when creating a purchase order.
/// </summary>
public class CreatePurchaseOrderLineDto
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}