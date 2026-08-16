namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// ميزان المراجعة: قائمة بأرصدة الحسابات مع إجمالي المدين والدائن.
/// الميزان متوازن إذا كان Σ المدين == Σ الدائن.
/// </summary>
public class TrialBalanceDto
{
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced => TotalDebit == TotalCredit;
    public List<TrialBalanceLineDto> Lines { get; set; } = new();
}