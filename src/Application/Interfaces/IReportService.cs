using ERPSystem.Application.DTOs.Reports;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for financial reports (التقارير المالية):
/// trial balance, income statement, and balance sheet.
/// </summary>
public interface IReportService
{
    /// <summary>ميزان المراجعة: أرصدة كل الحسابات مع إجمالي المدين والدائن.</summary>
    Task<TrialBalanceDto> GetTrialBalanceAsync();

    /// <summary>قائمة الدخل لفترة محددة (from/to).</summary>
    Task<IncomeStatementDto> GetIncomeStatementAsync(DateTime from, DateTime to);

    /// <summary>الميزانية العمومية كما في تاريخ محدد (asOf).</summary>
    Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOf);

    /// <summary>دفتر أستاذ حساب (الرصيد الافتتاحي والحركات والرصيد الختامي) لفترة محددة.</summary>
    Task<AccountLedgerDto> GetAccountLedgerAsync(Guid accountId, DateTime from, DateTime to);
}