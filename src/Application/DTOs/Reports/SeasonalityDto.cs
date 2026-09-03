namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// الموسمية: إجمالي المبيعات لكل سنة × شهر (من فواتير البيع غير الملغاة).
/// سلسلة لكل سنة (12 قيمة شهرية) لعرض مخطط أعمدة مجمّعة.
/// </summary>
public sealed class SeasonalityDto
{
    public List<int> Years { get; set; } = new();
    public List<SeasonalitySeries> Series { get; set; } = new();

    /// <summary>إجمالي كل سنة لإجمالي عمود في الجدول.</summary>
    public List<decimal> YearTotals { get; set; } = new();
    public decimal GrandTotal { get; set; }
}

/// <summary>سلسلة سنة واحدة: 12 قيمة شهرية (يناير ← ديسمبر).</summary>
public sealed class SeasonalitySeries
{
    public int Year { get; set; }
    public string Color { get; set; } = "";
    public decimal[] Months { get; set; } = new decimal[12];

    public decimal Total => Months.Sum();
}