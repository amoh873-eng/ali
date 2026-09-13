namespace ERPSystem.Domain.Entities;

/// <summary>
/// رصد سعر منافس (Shelf Price Capture): صورة + سعر مرصود + اسم المنافس لصنف — بيانات بسيطة
/// تُخزَّن للتحليل اللاحق (لا تتصل بأي منطق تسعير) — صُمِّمت دون تعقيد كما في الخطة.
/// </summary>
public class ShelfPriceCapture
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string CompetitorName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? PhotoPath { get; set; }
    public string? Notes { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}