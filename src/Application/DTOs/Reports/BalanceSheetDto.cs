namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// الميزانية العمومية (Balance Sheet): الأصول مقابل الخصوم وحقوق الملكية في تاريخ محدد.
/// المعادلة المحاسبية: الأصول = الخصوم + حقوق الملكية.
/// صافي الدخل يُدرج ضمن حقوق الملكية كأرباح محتجزة لضمان التوازن.
/// </summary>
public class BalanceSheetDto
{
    public DateTime AsOf { get; set; }
    public List<StatementLineDto> Assets { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public List<StatementLineDto> Liabilities { get; set; } = new();
    public decimal TotalLiabilities { get; set; }
    public List<StatementLineDto> Equity { get; set; } = new();
    public decimal TotalEquity { get; set; }
    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public bool IsBalanced => TotalAssets == TotalLiabilitiesAndEquity;
}