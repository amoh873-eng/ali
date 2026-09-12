using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements sales invoice business logic (فواتير المبيعات) with full accounting integration.
///
/// عند ترحيل فاتورة بيع ننفّذ ثلاثة آثار ذرياً (في نفس SaveChanges):
/// 1) حركة مخزون "صادر مبيعات" لكل بند مع التحقق من كفاية الرصيد.
/// 2) قيد محاسبي للبيع:
///    مدين: الصندوق (نقدي) أو العملاء (آجل) = TotalAmount
///    دائن: إيراد المبيعات = NetBeforeTax ، دائن: ضريبة المبيعات = TaxAmount
/// 3) قيد محاسبي للتكلفة:
///    مدين: تكلفة البضاعة المباعة = Σ(كمية × تكلفة الوحدة)
///    دائن: مخزون البضاعة = نفس المبلغ
/// لماذا نفصل قيد التكلفة عن قيد البيع؟ لأن كل ائتمان للسلعة له أثران محاسبيان
/// مستقلان: أثر "إيراد البيع" (زيادة الإيراد واستحقاق النقد/حق على العميل) وأثر
/// "خروج البضاعة من المخزون" (تحميل التكلفة). فصلهما يسهّل المطابقة والتدقيق.
/// </summary>
public partial class SalesInvoiceService : ISalesInvoiceService
{
    // أكواد الحسابات النظامية المزروعة في SeedSalesAccounts (يجب تطابقها تماماً)
    private const string AccountCash = "1100";
    private const string AccountReceivable = "1200";
    // ذمم البطاقات: مبالغ البطاقات التي أوفتها المنشأة للبنك وما زالت قيد التسوية (مستحق من البنك)
    private const string AccountCardReceivables = "1205";
    // درج الكاش: موقع نقدي منفصل عن الخزنة الرئيسية «الصندوق» — تُرحَّل إليه مبيعات نقطة البيع النقدية
    private const string AccountTillDrawer = "1105";
    private const string AccountInventory = "1300";
    private const string AccountSalesTaxPayable = "2100";
    private const string AccountSalesRevenue = "4100";
    private const string AccountCostOfGoodsSold = "5100";

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public SalesInvoiceService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    public async Task<SalesInvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _context.Set<SalesInvoice>()
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        return invoice is null ? null : MapToDto(invoice);
    }

    public async Task<List<SalesInvoiceDto>> GetInvoicesAsync()
    {
        var invoices = await _context.Set<SalesInvoice>()
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices.Select(MapToDto).ToList();
    }

    public async Task<decimal> GetAvailableStockAsync(Guid itemId, Guid warehouseId)
    {
        // الرصيد الحالي = مجموع الحركات (الموجبة/السالبة) للصنف في المخزن
        return await _context.Set<StockMovement>()
            .Where(m => m.ItemId == itemId && m.WarehouseId == warehouseId)
            .SumAsync(m => (decimal?)m.Quantity) ?? 0m;
    }
}