namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// حالة صف المطابقة بين فواتير البطاقة في النظام وكشف البنك المستورد.
/// </summary>
public enum CardReconciliationStatus
{
    /// <summary>مطابق: المرجع والمبلغ موجودان في النظام وعند البنك</summary>
    Matched = 1,

    /// <summary>في النظام بلا تأكيد بنكي بعد — يحتاج متابعة</summary>
    NoBankConfirmation = 2,

    /// <summary>عند البنك وليس في النظام — يحتاج تحقيق</summary>
    NotInSystem = 3,

    /// <summary>المرجع موجود عند الطرفين لكن المبلغ مختلف — يحتاج تدقيق</summary>
    AmountMismatch = 4
}

/// <summary>
/// صف واحد في شاشة تسوية بطاقات الدفع (Card Reconciliation).
/// </summary>
public class CardReconciliationRowDto
{
    public string Reference { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? Last4 { get; set; }
    public string? Network { get; set; }
    public decimal? SystemAmount { get; set; }
    public DateTime? SystemDate { get; set; }
    public decimal? BankAmount { get; set; }
    public DateTime? BankDate { get; set; }
    public CardReconciliationStatus Status { get; set; }

    public string StatusNameAr => Status switch
    {
        CardReconciliationStatus.Matched => "مطابق",
        CardReconciliationStatus.NoBankConfirmation => "بانتظار تأكيد البنك",
        CardReconciliationStatus.NotInSystem => "غير موجود في النظام",
        _ => "اختلاف بالمبلغ"
    };

    /// <summary>هل المبلغان (إن وجدا) متساويان ضمن فارق 0.01؟</summary>
    public bool AmountsMatch
    {
        get
        {
            if (SystemAmount.HasValue && BankAmount.HasValue)
                return Math.Abs(SystemAmount.Value - BankAmount.Value) <= 0.01m;
            return true;
        }
    }
}

/// <summary>
/// نتيجة شاشة التسوية: الصفوف + ملخص سريع.
/// </summary>
public class CardReconciliationResultDto
{
    public List<CardReconciliationRowDto> Rows { get; set; } = new();
    public int MatchedCount { get; set; }
    public int PendingCount { get; set; }
    public int BankOnlyCount { get; set; }
    public int MismatchCount { get; set; }
    public decimal MatchedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal BankOnlyAmount { get; set; }
    public decimal MismatchAmount { get; set; }
    public decimal SystemCardTotal { get; set; }
    public decimal BankImportedTotal { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

/// <summary>
/// نتيجة استيراد كشف البنك (عدد ما استُورد/تخطّى/أخطاء).
/// </summary>
public class CardBankImportResultDto
{
    public int TotalRows { get; set; }
    public int ImportedNew { get; set; }
    public int SkippedExisting { get; set; }
    public List<string> Errors { get; set; } = new();
    public decimal ImportedAmount { get; set; }
}