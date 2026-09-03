namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// تقرير ضريبة المبيعات (Sales Tax Report) — يجمع فواتير المبيعات في الفترة
/// حسب شرائح الضريبة (نسبة الضريبة) ويعرض لكل شريحة: عدد الفواتير، إجمالي
/// المبيعات، مبلغ الضريبة، مع إجمالي مدين/دائن من القيود المحاسبية المرتبطة
/// للتحقق من توازن القيود (Debit == Credit).
/// </summary>
public sealed class SalesTaxReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    /// <summary>صفوف الشرائح الضريبية مرتبة تنازلياً حسب النسبة.</summary>
    public List<SalesTaxReportRow> Rows { get; set; } = new();

    /// <summary>إجمالي عدد الفواتير في الفترة.</summary>
    public int TotalInvoices => Rows.Sum(r => r.InvoiceCount);

    /// <summary>إجمالي المبيعات (الأساس الخاضع للضريبة بدون الضريبة).</summary>
    public decimal TotalGross => Rows.Sum(r => r.GrossAmount);

    /// <summary>إجمالي مبلغ الضريبة.</summary>
    public decimal TotalTax => Rows.Sum(r => r.TaxAmount);

    /// <summary>إجمالي المبيعات شامل الضريبة (Gross + Tax).</summary>
    public decimal TotalSalesInclTax => TotalGross + TotalTax;

    /// <summary>إجمالي المدين من القيود المحاسبية لفواتير الفترة (البيع).</summary>
    public decimal TotalDebit { get; set; }

    /// <summary>إجمالي الدائن من القيود المحاسبية لفواتير الفترة (البيع).</summary>
    public decimal TotalCredit { get; set; }

    /// <summary>التحقق من توازن القيود (Debit == Credit) للتأكيد المحاسبي.</summary>
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
}

/// <summary>صف شريحة ضريبية واحدة في تقرير ضريبة المبيعات.</summary>
public sealed class SalesTaxReportRow
{
    /// <summary>نسبة الضريبة (16، 10، 5، 4، 2، 0).</summary>
    public decimal TaxRate { get; set; }

    /// <summary>وصف الشريحة الضريبية (مثال: "ضريبة مبيعات 16% مستحقة").</summary>
    public string TaxDescription { get; set; } = string.Empty;

    /// <summary>عدد فواتير المبيعات ضمن هذه الشريحة.</summary>
    public int InvoiceCount { get; set; }

    /// <summary>إجمالي المبيعات الخاضعة للضريبة (أساس الضريبة = SubTotal - خصم).</summary>
    public decimal GrossAmount { get; set; }

    /// <summary>إجمالي مبلغ الضريبة المحتسبة.</summary>
    public decimal TaxAmount { get; set; }
}