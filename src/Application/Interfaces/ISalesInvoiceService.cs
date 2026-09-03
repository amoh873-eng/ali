using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Application.DTOs.Sales;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for sales invoice operations (فواتير المبيعات).
/// The create method also performs the accounting integration:
/// stock issue movements + automatic balanced journal entries.
/// </summary>
public interface ISalesInvoiceService
{
    /// <summary>
    /// Returns all invoices (newest first) with customer/warehouse names.
    /// </summary>
    Task<List<SalesInvoiceDto>> GetInvoicesAsync();

    /// <summary>
    /// Gets a single invoice including its lines.
    /// </summary>
    Task<SalesInvoiceDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates and posts a new sales invoice: validates, issues stock,
    /// and generates the balanced sales + COGS journal entries. Atomic (single SaveChanges).
    /// </summary>
    Task<SalesInvoiceDto> CreateAsync(CreateSalesInvoiceDto dto);

    /// <summary>
    /// Gets the current stock balance of an item in a warehouse
    /// (used by the UI to warn about insufficient stock).
    /// </summary>
    Task<decimal> GetAvailableStockAsync(Guid itemId, Guid warehouseId);

    /// <summary>أفضل 5 أصناف مبيعاً حسب إجمالي المبلغ (للداشبورد).</summary>
    Task<List<TopSellingItemDto>> GetTopSellingItemsAsync(int count = 5);

    /// <summary>
    /// تسوية مبيعات البطاقات مع كشف البنك للفترة المحددة:
    /// يطابق كل فاتورة بطاقة في النظام مع صفوف البنك المستوردة عبر رقم المرجع والمبلغ.
    /// </summary>
    Task<CardReconciliationResultDto> GetCardReconciliationAsync(DateTime from, DateTime to);

    /// <summary>
    /// يستورد كشف تسوية البنك (CSV أو XLSX) ويدمج صفوفه مع حالته الحالية.
    /// </summary>
    Task<CardBankImportResultDto> ImportBankStatementAsync(Stream fileStream, string fileName, string? importedBy);
}
