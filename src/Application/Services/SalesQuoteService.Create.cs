using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// SalesQuoteService — الإنشاء والاعتماد والإلغاء.
/// </summary>
public partial class SalesQuoteService
{
    public async Task<SalesQuoteDto> CreateAsync(CreateSalesQuoteDto dto)
    {
        // 1) التحقق من العميل
        var customer = await _context.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && !c.IsDeleted);
        if (customer is null)
            throw new InvalidOperationException("العميل غير موجود");

        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new InvalidOperationException("يجب إضافة بند واحد على الأقل.");

        // 2) جلب الأصناف مرة واحدة والتحقق من وجودها وعدم تكرارها
        var distinctItemIds = dto.Lines.Select(l => l.ItemId).Distinct().ToList();
        if (distinctItemIds.Count != dto.Lines.Count)
            throw new InvalidOperationException("لا يمكن تكرار نفس الصنف في عرض واحد.");

        var items = await _context.Set<Item>()
            .Where(i => distinctItemIds.Contains(i.Id) && !i.IsDeleted)
            .ToListAsync();
        if (items.Count != distinctItemIds.Count)
            throw new InvalidOperationException("أحد الأصناف المحددة غير موجود.");

        // 3) بناء العرض وحساب المجاميع (نفس نموذج الخصم والضريبة في الفواتير)
        var quote = new SalesQuote
        {
            Id = Guid.NewGuid(),
            QuoteNumber = await NextQuoteNumberAsync(),
            CustomerId = customer.Id,
            QuoteDate = dto.QuoteDate,
            ExpiryDate = dto.ExpiryDate,
            Status = QuoteStatus.Draft,
            DiscountPercentage = dto.DiscountPercentage,
            TaxRate = dto.TaxRate,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        var subTotal = 0m;
        var lines = new List<SalesQuoteLine>();
        foreach (var input in dto.Lines)
        {
            if (input.Quantity <= 0)
                throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");

            var line = new SalesQuoteLine
            {
                Id = Guid.NewGuid(),
                SalesQuoteId = quote.Id,
                ItemId = input.ItemId,
                Quantity = input.Quantity,
                UnitPrice = input.UnitPrice,
                LineTotal = Math.Round(input.Quantity * input.UnitPrice, 2),
                CreatedAt = DateTime.UtcNow
            };
            lines.Add(line);
            subTotal += line.LineTotal;
        }

        quote.SubTotal = Math.Round(subTotal, 2);
        quote.DiscountAmount = Math.Round(quote.SubTotal * (dto.DiscountPercentage / 100m), 2);
        var netBeforeTax = quote.SubTotal - quote.DiscountAmount;
        quote.TaxAmount = Math.Round(netBeforeTax * (dto.TaxRate / 100m), 2);
        quote.TotalAmount = netBeforeTax + quote.TaxAmount;
        quote.Lines = lines;

        _context.Set<SalesQuote>().Add(quote);
        _context.Set<SalesQuoteLine>().AddRange(lines);

        await _context.SaveChangesAsync();

        return MapToDto(quote);
    }

    public async Task ApproveAsync(Guid id)
    {
        var quote = await GetEntityAsync(id);
        if (quote.Status != QuoteStatus.Draft)
            throw new InvalidOperationException("لا يمكن اعتماد عرض غير مسودة.");
        quote.Status = QuoteStatus.Approved;
        quote.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(Guid id)
    {
        var quote = await GetEntityAsync(id);
        if (quote.Status == QuoteStatus.Converted)
            throw new InvalidOperationException("لا يمكن إلغاء عرض تم تحويله إلى فاتورة.");
        quote.Status = QuoteStatus.Cancelled;
        quote.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}