namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// بند في ميزان المراجعة: حساب مع إجمالي مدين ودائن وصافي رصيده.
/// </summary>
public class TrialBalanceLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountNameAr { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }

    /// <summary>صافي الرصيد (مدين - دائن)؛ موجب = مدين، سالب = دائن.</summary>
    public decimal Balance { get; set; }
}