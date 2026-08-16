namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// سطر مالي عام (يُستخدم في قائمة الدخل والميزانية العمومية).
/// يمثل حساباً أو مجمّعاً بمبلغ واحد.
/// </summary>
public class StatementLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountNameAr { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}