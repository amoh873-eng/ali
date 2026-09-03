namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// قائمة التدفق النقدي (بصيغة غير مباشرة) — ثالث القوائم المالية الأساسية.
/// تُبنى من بنود القيود المحاسبية فقط (صافي الدخل + تغيّر رأس المال العامل).
/// </summary>
public sealed class CashFlowStatementDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    /// <summary>أقسام التدفق (تشغيلي/استثماري/تمويلي) — كل قسم قائمة بنود.</summary>
    public List<CashFlowSection> Sections { get; set; } = new();

    public decimal NetChangeCash { get; set; }
    public decimal BeginningCash { get; set; }
    public decimal EndingCash { get; set; }

    /// <summary>رصيد النقدية الفعلي من دفتر الأستاذ في نهاية الفترة (للتحقق).</summary>
    public decimal GlEndingCash { get; set; }

    /// <summary>متوازن إذا تطابق الرصيد المحسوب مع رصيد دفتر الأستاذ.</summary>
    public bool IsBalanced => Math.Abs(EndingCash - GlEndingCash) < 0.01m;
}

/// <summary>قسم في قائمة التدفق: عنوان + بنود + إجمالي فرعي.</summary>
public sealed class CashFlowSection
{
    public string Key { get; set; } = "";   // operating | investing | financing
    public string TitleKey { get; set; } = "";
    public List<CashFlowLine> Lines { get; set; } = new();
    public decimal Subtotal { get; set; }
}

/// <summary>بند تدفق نقدي (وصف/تسمية + مبلغ).</summary>
public sealed class CashFlowLine
{
    public string DescriptionKey { get; set; } = "";
    public decimal Amount { get; set; }
}