namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for a sales invoice line item (بند في فاتورة بيع).
/// </summary>
public class SalesInvoiceLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemNameAr { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}
