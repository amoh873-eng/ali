using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// SalesQuoteService — التحويل إلى فاتورة والأدوات المساعدة.
/// </summary>
public partial class SalesQuoteService
{
    public async Task<SalesQuoteDto> ConvertToInvoiceAsync(Guid id, Guid warehouseId, int invoiceType)
    {
        var quote = await GetEntityAsync(id);
        if (quote.Status == QuoteStatus.Converted)
            throw new InvalidOperationException("هذا العرض محوّل إلى فاتورة بالفعل.");
        if (quote.Status == QuoteStatus.Cancelled)
            throw new InvalidOperationException("لا يمكن تحويل عرض ملغي.");

        // نبني DTO الفاتورة من بنود العرض ثم نستدعي خدمة الفواتير.
        // لماذا نستدعي خدمة الفواتير بدل تكرار منطقها هنا؟
        // حتى يبقى منطق الترحيل (حركات المخزون + القيود المحاسبية) في مكان واحد،
        // فلا تتكرر القواعد المحاسبية في موضعين وتتباعد لاحقاً.
        var invoiceDto = await _invoiceService.CreateAsync(new CreateSalesInvoiceDto
        {
            CustomerId = quote.CustomerId,
            WarehouseId = warehouseId,
            InvoiceDate = DateTime.Today,
            InvoiceType = invoiceType,
            DiscountPercentage = quote.DiscountPercentage,
            TaxRate = quote.TaxRate,
            Note = $"محوّل من عرض سعر {quote.QuoteNumber}",
            Lines = quote.Lines.Select(l => new CreateSalesInvoiceLineDto
            {
                ItemId = l.ItemId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        });

        // ربط العرض بالفاتورة الناتجة وإغلاقه
        quote.ConvertedInvoiceId = invoiceDto.Id;
        quote.Status = QuoteStatus.Converted;
        quote.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(quote);
    }

    private async Task<SalesQuote> GetEntityAsync(Guid id)
    {
        var quote = await _context.Set<SalesQuote>()
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);
        if (quote is null)
            throw new InvalidOperationException("العرض غير موجود.");
        return quote;
    }

    private Task<string> NextQuoteNumberAsync()
        => NumberSequenceHelper.NextAsync(_context, "SQ");

    private static SalesQuoteDto MapToDto(SalesQuote quote)
    {
        return new SalesQuoteDto
        {
            Id = quote.Id,
            QuoteNumber = quote.QuoteNumber,
            CustomerId = quote.CustomerId,
            CustomerCode = quote.Customer?.Code ?? "—",
            CustomerName = quote.Customer?.NameAr ?? "—",
            QuoteDate = quote.QuoteDate,
            ExpiryDate = quote.ExpiryDate,
            Status = (int)quote.Status,
            SubTotal = quote.SubTotal,
            DiscountPercentage = quote.DiscountPercentage,
            DiscountAmount = quote.DiscountAmount,
            TaxRate = quote.TaxRate,
            TaxAmount = quote.TaxAmount,
            TotalAmount = quote.TotalAmount,
            Note = quote.Note,
            ConvertedInvoiceId = quote.ConvertedInvoiceId,
            Lines = quote.Lines.Select(MapLineToDto).ToList()
        };
    }

    private static SalesQuoteLineDto MapLineToDto(SalesQuoteLine line)
    {
        return new SalesQuoteLineDto
        {
            Id = line.Id,
            ItemId = line.ItemId,
            ItemCode = line.Item?.Code ?? "—",
            ItemNameAr = line.Item?.NameAr ?? "—",
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineTotal = line.LineTotal
        };
    }
}