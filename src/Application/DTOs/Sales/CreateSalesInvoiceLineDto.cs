namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for a single sales invoice line used as INPUT when creating/posting an invoice.
/// The user selects an item and a quantity; UnitPrice defaults to the item's sale price.
/// </summary>
public class CreateSalesInvoiceLineDto
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
