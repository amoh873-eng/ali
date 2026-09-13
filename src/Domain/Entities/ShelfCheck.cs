namespace ERPSystem.Domain.Entities;

/// <summary>
/// فحص رف (Shelf Check): كمية صنف ملاحَظة فعلياً على الرف في موقع/متجر، مع صور قبل/بعد اختيارية.
/// يُسجَّل ميدانياً على جوال منسّق الرفوف ويغذّي مرحلة 5 (تكوّرات النفاد المتكرر).
/// </summary>
public class ShelfCheck
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal ObservedQty { get; set; }
    public decimal? ParLevel { get; set; }
    public string? PhotoOnePath { get; set; }
    public string? PhotoTwoPath { get; set; }
    public string? Notes { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}