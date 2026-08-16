using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements sales return business logic (مردودات المبيعات) — يعكس أثر الفاتورة الأصلية.
///
/// عند ترحيل مردود مبيعات ننفّذ ذرياً:
/// 1) حركة مخزون "وارد مردود" لإعادة البضاعة إلى المخزن.
/// 2) قيد عكسي للبيع:
///    مدين: مردودات المبيعات = NetBeforeTax ، مدين: ضريبة المبيعات = TaxAmount
///    دائن: الصندوق/العملاء = TotalAmount
/// 3) قيد عكسي للتكلفة:
///    مدين: مخزون البضاعة = Σ(كمية × تكلفة) ، دائن: تكلفة البضاعة المباعة = نفس المبلغ
/// لماذا العكس بهذا الشكل؟ المردود يعني "إلغاء" البيع، فنسجّل القيد المعاكس تماماً
/// حتى يعود كل حساب إلى حالته التي كان عليها لو لم يحدث البيع أصلاً.
/// </summary>
public partial class SalesReturnService : ISalesReturnService
{
    private const string AccountCash = "1100";
    private const string AccountReceivable = "1200";
    private const string AccountInventory = "1300";
    private const string AccountSalesTaxPayable = "2100";
    private const string AccountSalesReturns = "4110";
    private const string AccountCostOfGoodsSold = "5100";

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public SalesReturnService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    public async Task<List<SalesReturnDto>> GetReturnsAsync()
    {
        var returns = await _context.Set<SalesReturn>()
            .Include(r => r.Customer)
            .Include(r => r.SalesInvoice)
            .Include(r => r.Warehouse)
            .OrderByDescending(r => r.ReturnDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        return returns.Select(MapToDto).ToList();
    }

    public async Task<SalesReturnDto?> GetByIdAsync(Guid id)
    {
        var salesReturn = await _context.Set<SalesReturn>()
            .Include(r => r.Customer)
            .Include(r => r.SalesInvoice)
            .Include(r => r.Warehouse)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        return salesReturn is null ? null : MapToDto(salesReturn);
    }
}