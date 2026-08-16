namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for displaying a single sales quote line.
/// </summary>
public class SalesQuoteLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemNameAr { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}