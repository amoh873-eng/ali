namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// تقرير المؤشرات المالية — كل مؤشر يُعرض للفترة الحالية وللفترة السابقة
/// (للمقارنة) مع مؤشر اتجاه صعود/هبوط.
/// </summary>
public sealed class FinancialRatiosDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    /// <summary>المؤشرات المجمعة حسب الفئة (سيولة، ربحية، كفاءة تشغيل).</summary>
    public List<RatioCategory> Categories { get; set; } = new();
}

/// <summary>فئة مؤشرات (سيولة/ربحية/كفاءة) تتضمن مجموعة مؤشرات.</summary>
public sealed class RatioCategory
{
    public string Key { get; set; } = "";      // "liquidity" / "profitability" / "efficiency"
    public List<FinancialRatioItem> Items { get; set; } = new();
}

/// <summary>بند مؤشر واحد: القيمة الحالية + السابقة + اتجاه + شرح مُترجم.</summary>
public sealed class FinancialRatioItem
{
    public string Key { get; set; } = "";      // "currentRatio" / "quickRatio" / ...
    public decimal? CurrentValue { get; set; } // null = غير قابل للحساب (قسمة على صفر/لا بيانات)
    public decimal? PreviousValue { get; set; }

    /// <summary>
    /// الاتجاه يُحسب كمُفضَّل: صعود لبعض المؤشرات (مثل نسبة السيولة) وهبوط لبعضها
    /// (مثل DSO). قيم النسبة تُقارن مباشرة، والمؤشرات "كلما قلّت أفضل" تُعكس.
    /// </summary>
    public bool IsPositiveMove { get; set; }
}