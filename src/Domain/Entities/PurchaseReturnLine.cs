using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// بند في مردود مشتريات: صنف بكمية مردودة وتكلفة وحدة.
/// </summary>
public class PurchaseReturnLine : BaseEntity
{
    public Guid PurchaseReturnId { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}