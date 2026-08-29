namespace ERPSystem.Application.DTOs.Inventory;

/// <summary>كائن عرض لدفعة مخزون (العرض في صفحة تتبّع انتهاء الصلاحية).</summary>
public class StockBatchDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemNameAr { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public DateTime ReceivedDate { get; set; }

    /// <summary>حالة العرض المحسوبة (نفس معايير التحقق في الخدمة الخلفية).</summary>
    public string Status { get; set; } = "Fine";
}