namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>DTO لبند (إدخال) في فاتورة مشتريات.</summary>
public class CreatePurchaseInvoiceLineDto
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }

    /// <summary>تاريخ انتهاء صلاحية الدفعة — إلزامي عندما يكون الصنف TracksExpiry.</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>رقم الدفعة/الشحنة من المورد (اختياري).</summary>
    public string? BatchNumber { get; set; }
}