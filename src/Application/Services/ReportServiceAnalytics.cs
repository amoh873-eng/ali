using ERPSystem.Application.DTOs.Reports;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

public partial class ReportService
{
    /// <summary>تقرير أعمار الذمم — يجمع حسب الطرف (انظر AgingReportQueries).</summary>
    public Task<AgingReportDto> GetAgingReportAsync(bool isReceivable, DateTime asOf)
        => AgingReportQueries.GetAgingAsync(_context, isReceivable, asOf);

    /// <summary>أعلى N عملاء بنسبة من المبيعات + تحذير تركيز &gt;70%.</summary>
    public Task<(List<AgingReportQueries.TopCustomerRow> Top, bool Warning)>
        GetTopCustomersAsync(DateTime asOf, int topN = 5)
        => AgingReportQueries.GetTopCustomersAsync(_context, asOf, topN);

    /// <summary>المؤشرات المالية للفترة مع مقارنة الفترة السابقة.</summary>
    public Task<FinancialRatiosDto> GetFinancialRatiosAsync(DateTime from, DateTime to)
        => FinancialRatiosService.ComputeAsync(_context, from, to);

    /// <summary>قائمة التدفق النقدي (بصيغة غير مباشرة) — قراءة فقط من القيود.</summary>
    public Task<CashFlowStatementDto> GetCashFlowStatementAsync(DateTime from, DateTime to)
        => CashFlowService.ComputeAsync(_context, from, to);

    /// <summary>ربحية الأصناف/الفئات — هامش المساهمة (قراءة فقط).</summary>
    public Task<ItemProfitabilityDto> GetItemProfitabilityAsync(DateTime from, DateTime to)
        => OperationalAnalyticsService.GetItemProfitabilityAsync(_context, from, to);

    /// <summary>الأصناف البطيئة/الخاملة (لا مبيع خلال آخر N يوم) — قراءة فقط.</summary>
    public Task<SlowMovingDto> GetSlowMovingAsync(int daysThreshold)
        => OperationalAnalyticsService.GetSlowMovingAsync(_context, daysThreshold);

    /// <summary>تصنيف ABC على أساس قيمة المخزون — قراءة فقط.</summary>
    public Task<AbcAnalysisDto> GetAbcAnalysisAsync()
        => OperationalAnalyticsService.GetAbcAnalysisAsync(_context);

    /// <summary>الموسمية: مبيعات كل سنة × 12 شهراً — قراءة فقط.</summary>
    public Task<SeasonalityDto> GetSeasonalityAsync()
        => SeasonalityService.ComputeAsync(_context);
}