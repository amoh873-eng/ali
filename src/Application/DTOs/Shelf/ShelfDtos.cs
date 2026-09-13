namespace ERPSystem.Application.DTOs.Shelf;

/// <summary>إدخال فحص رف جديد (الجوال).</summary>
public class CreateShelfCheckDto
{
    public Guid? ItemId { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal ObservedQty { get; set; }
    public decimal? ParLevel { get; set; }
    public string? PhotoOneBase64 { get; set; }
    public string? PhotoTwoBase64 { get; set; }
    public string? Notes { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.Now;
}

/// <summary>عرض فحص رف.</summary>
public class ShelfCheckDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal ObservedQty { get; set; }
    public decimal? ParLevel { get; set; }
    public string? PhotoOnePath { get; set; }
    public string? PhotoTwoPath { get; set; }
    public string? Notes { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}

/// <summary>نموذج حد رف مستهدَف (عرض).</summary>
public class ShelfParLevelDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal ParLevel { get; set; }
}

/// <summary>إدخال رصد سعر منافس.</summary>
public class CreateShelfPriceCaptureDto
{
    public Guid? ItemId { get; set; }
    public string CompetitorName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? PhotoBase64 { get; set; }
    public string? Notes { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.Now;
}

/// <summary>عرض رصد سعر منافس.</summary>
public class ShelfPriceCaptureDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string CompetitorName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? PhotoPath { get; set; }
    public string? Notes { get; set; }
    public DateTime CapturedAt { get; set; }
}

/// <summary>تلخيص النشاط الميداني لعضو (فحوصات + رصد أسعار) — تغذية لوحة المدير.</summary>
public class ShelfFieldActivityDto
{
    public string UserId { get; set; } = string.Empty;
    public int ShelfChecks { get; set; }
    public int PriceCaptures { get; set; }
    public int RepCollections { get; set; }
}

/// <summary>صنف يُلاحَظ تحت حد رفه في موقع مراتٍ متكررة (مؤشر مشكلة تزويد).</summary>
public class ShelfShortfallDto
{
    public Guid ItemId { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal ParLevel { get; set; }
    public int BelowCount { get; set; }
    public decimal AvgObserved { get; set; }
}
public class ShelfExpiryWarningDto
{
    public Guid? ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemNameAr { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public decimal RemainingQty { get; set; }
    public int DaysLeft { get; set; }
}