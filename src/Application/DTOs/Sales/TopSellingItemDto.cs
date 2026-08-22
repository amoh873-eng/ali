namespace ERPSystem.Application.DTOs.Sales;

public class TopSellingItemDto
{
    public Guid ItemId { get; set; }
    public string ItemNameAr { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}
