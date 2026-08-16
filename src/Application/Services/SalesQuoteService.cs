using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements sales quote business logic (عروض الأسعار).
///
/// الفكرة المحورية: العرض مستند "غير مُرحَّل" (Non-Posting). كل عملياته
/// (إنشاء/اعتماد/إلغاء) لا تمسّ المخزون ولا الحسابات نهائياً. الأثر الوحيد يحدث
/// عند التحويل (ConvertToInvoiceAsync) حيث نستدعي خدمة الفواتير التي ترحّل فاتورة
/// حقيقية: تصدر حركات مخزون "صادر" وتنشئ القيود المحاسبية المتوازنة.
/// </summary>
public partial class SalesQuoteService : ISalesQuoteService
{
    private readonly DbContext _context;
    private readonly ISalesInvoiceService _invoiceService;

    public SalesQuoteService(DbContext context, ISalesInvoiceService invoiceService)
    {
        _context = context;
        _invoiceService = invoiceService;
    }

    public async Task<SalesQuoteDto?> GetByIdAsync(Guid id)
    {
        var quote = await _context.Set<SalesQuote>()
            .Include(q => q.Customer)
            .Include(q => q.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);

        return quote is null ? null : MapToDto(quote);
    }

    public async Task<List<SalesQuoteDto>> GetQuotesAsync()
    {
        var quotes = await _context.Set<SalesQuote>()
            .Include(q => q.Customer)
            .Include(q => q.Lines).ThenInclude(l => l.Item)
            .OrderByDescending(q => q.QuoteDate)
            .ThenByDescending(q => q.CreatedAt)
            .ToListAsync();

        return quotes.Select(MapToDto).ToList();
    }
}