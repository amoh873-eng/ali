using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for a single sales-return line (INPUT) when creating a return.
/// </summary>
public class CreateSalesReturnLineDto
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
