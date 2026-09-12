namespace ERPSystem.Application.DTOs.Inventory;

/// <summary>نموذج إدخال سند إتلاف جديد (تُنشأ عبر الخدمة ضمن معاملة ذرية مع القيد المحاسبي).</summary>
public class CreateStockWriteOffDto
{
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }

    /// <summary>الدُفعة المحددة المراد إتلافها — إلزامية فقط لو كان الصنف يتتبّع دُفعات (Item.TracksBatches).</summary>
    public Guid? BatchId { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>قيمة رقمية من StockWriteOffReason (منتهي الصلاحية / تالف / سرقة أو فقد / أخرى).</summary>
    public int Reason { get; set; }

    /// <summary>نص حر شرطي — يلزم فقط عند السبب "أخرى".</summary>
    public string? OtherReasonText { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}

/// <summary>كائن عرض لسند إتلاف مع بيانات الصلة (صنف/مخزن/دُفعة/قيد).</summary>
public class StockWriteOffDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;

    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemNameAr { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }
    public string WarehouseNameAr { get; set; } = string.Empty;

    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public decimal Quantity { get; set; }
    public int Reason { get; set; }
    public string ReasonNameAr { get; set; } = string.Empty;
    public string? OtherReasonText { get; set; }

    public DateTime Date { get; set; }
    public decimal UnitCost { get; set; }

    /// <summary>قيمة السند = الكمية × سعر التكلفة المسجَّل (تحسب دائماً من نفس الحقلين المثبّتين).</summary>
    public decimal TotalCost => Math.Round(Quantity * UnitCost, 2);

    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }

    /// <summary>معلومات القيد المحاسبي المرتبط (لعرضه في القائمة والتفاصيل).</summary>
    public Guid JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string EntryDescription { get; set; } = string.Empty;
}

/// <summary>خيار دُفعة لعرضها في نموذج سند الإتلاف (للصنف الذي يتتبّع دُفعات).</summary>
public class ItemBatchOptionDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>حالة الدُفعة (Fine / Expired / Expiring) للتلوين في القائمة.</summary>
    public string Status { get; set; } = "Fine";
}

/// <summary>صف تجميع (حسب السبب) في التقرير الملخّص.</summary>
public class StockWriteOffReasonSummaryDto
{
    public int Reason { get; set; }
    public string ReasonNameAr { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
}

/// <summary>صف تجميع شهري في التقرير الملخّص.</summary>
public class StockWriteOffMonthlyDto
{
    /// <summary>مفتاح الشهر بصيغة yyyy-MM.</summary>
    public string Month { get; set; } = string.Empty;

    /// <summary>تسمية العرض (مثال: سبتمبر 2026).</summary>
    public string MonthLabel { get; set; } = string.Empty;

    public decimal Value { get; set; }
}

/// <summary>التقرير الملخّص لقيمة الهالك (إجمالي + حسب السبب + حسب الشهر).</summary>
public class StockWriteOffSummaryDto
{
    public int Count { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public List<StockWriteOffReasonSummaryDto> ByReason { get; set; } = new();
    public List<StockWriteOffMonthlyDto> ByMonth { get; set; } = new();
}