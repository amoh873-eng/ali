namespace ERPSystem.Application.DTOs.Pos;

/// <summary>إدخال لحفظ عملية بيع موقوفة (Hold).</summary>
public class SaveHeldSaleDto
{
    /// <summary>معرّف أمين الصندوق الذي يوقف العملية.</summary>
    public string CashierUserId { get; set; } = string.Empty;

    /// <summary>معرّف العميل (اختياري).</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>لقطة JSON للسلة كاملة.</summary>
    public string LinesJson { get; set; } = string.Empty;

    /// <summary>ملاحظة اختيارية.</summary>
    public string? Note { get; set; }
}