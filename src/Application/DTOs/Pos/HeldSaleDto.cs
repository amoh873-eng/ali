namespace ERPSystem.Application.DTOs.Pos;

/// <summary>كائن عرض لعملية بيع موقوفة.</summary>
public class HeldSaleDto
{
    public Guid Id { get; set; }
    public string CashierUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string LinesJson { get; set; } = string.Empty;
    public string? Note { get; set; }

    /// <summary>هل العملية قديمة (أكبر من 24 ساعة) لتمييزها بصرياً دون حذفها؟</summary>
    public bool IsStale { get; set; }
}