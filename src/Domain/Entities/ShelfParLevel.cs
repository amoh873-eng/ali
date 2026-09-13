namespace ERPSystem.Domain.Entities;

/// <summary>
/// حد الرف المستهدَف (Shelf Par Level): صنف + موقع/متجر + الحد الأدنى المستهدَف على الرف.
/// يُستعمل في فحص الرفوف لتنبيه منسّق الرفوف عند نزول الكمية الملاحَظة دون هذا الحد.
/// </summary>
public class ShelfParLevel
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal ParLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}