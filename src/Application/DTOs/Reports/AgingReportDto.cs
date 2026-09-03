namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// تقرير أعمار الذمم (AR أو AP) — يجمع الفواتير المفتوحة حسب العميل/المورد
/// في فئات عمرية (0-30، 31-60، 61-90، +90) مع إجماليات كل عمود.
/// </summary>
public sealed class AgingReportDto
{
    public DateTime AsOf { get; set; }

    /// <summary>true = أعمار العملاء (AR) ، false = أعمار الموردين (AP).</summary>
    public bool IsReceivable { get; set; }

    public List<AgingPartyRow> Rows { get; set; } = new();

    public decimal Total0_30 { get; set; }
    public decimal Total31_60 { get; set; }
    public decimal Total61_90 { get; set; }
    public decimal Total90Plus { get; set; }
    public decimal TotalAll => Total0_30 + Total31_60 + Total61_90 + Total90Plus;
}

/// <summary>صف طرف (عميل/مورد) في تقرير الأعمار + فواتيره المفتوحة للتعمّق.</summary>
public sealed class AgingPartyRow
{
    public Guid PartyId { get; set; }
    public string PartyCode { get; set; } = string.Empty;
    public string PartyNameAr { get; set; } = string.Empty;

    /// <summary>0-30 يوم من الاستحقاق.</summary>
    public decimal Bucket0_30 { get; set; }
    /// <summary>31-60 يوم.</summary>
    public decimal Bucket31_60 { get; set; }
    /// <summary>61-90 يوم.</summary>
    public decimal Bucket61_90 { get; set; }
    /// <summary>أكثر من 90 يوم.</summary>
    public decimal Bucket90Plus { get; set; }

    public decimal Total => Bucket0_30 + Bucket31_60 + Bucket61_90 + Bucket90Plus;

    /// <summary>الفواتير المفتوحة المكوّنة للصف (للتعمّق عند الطلب).</summary>
    public List<AgingInvoiceLine> Invoices { get; set; } = new();
}

/// <summary>سطر فاتورة مفتوحة داخل عمق الأعمار.</summary>
public sealed class AgingInvoiceLine
{
    public Guid PartyId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public decimal Balance { get; set; }
}