namespace ERPSystem.Application.DTOs.Reports;

/// <summary>ربحية الأصناف/الفئات: إيراد وتكلفة وهامش مساهمة (من سطور الفواتير).</summary>
public sealed class ItemProfitabilityDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<ItemProfitabilityRow> Rows { get; set; } = new();
}

public sealed class ItemProfitabilityRow
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemNameAr { get; set; } = "";
    public string CategoryNameAr { get; set; } = "";
    public decimal QtySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Margin { get; set; }
    public decimal MarginPct { get; set; }
}

/// <summary>الأصناف البطيئة الحركة / الميتة في المخزون (رصيد أعلى من صفر بلا مبيع).</summary>
public sealed class SlowMovingDto
{
    public int DaysThreshold { get; set; }
    public List<SlowMovingRow> Rows { get; set; } = new();
}

public sealed class SlowMovingRow
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemNameAr { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal StockValue { get; set; }
    public bool NeverSold { get; set; }
    public int? DaysSinceLastSale { get; set; }
}

/// <summary>تصنيف ABC على أساس قيمة المخزون (كمية × تكلفة) مع النسب التراكمية.</summary>
public sealed class AbcAnalysisDto
{
    public decimal TotalValue { get; set; }
    public List<AbcAnalysisRow> Rows { get; set; } = new();
}

public sealed class AbcAnalysisRow
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemNameAr { get; set; } = "";
    public decimal Value { get; set; }
    public decimal SharePct { get; set; }
    public decimal CumulativePct { get; set; }
    public string Tier { get; set; } = ""; // A | B | C
}