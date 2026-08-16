namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for a single line item when creating a sales quote.
/// </summary>
public class CreateSalesQuoteLineDto
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}