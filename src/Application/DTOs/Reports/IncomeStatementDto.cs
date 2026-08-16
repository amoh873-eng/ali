namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// قائمة الدخل (Income Statement): الإيرادات مقابل المصروفات لفترة محددة.
/// صافي الدخل = الإيرادات - المصروفات.
/// </summary>
public class IncomeStatementDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<StatementLineDto> Revenues { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public List<StatementLineDto> Expenses { get; set; } = new();
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome => TotalRevenue - TotalExpenses;
}