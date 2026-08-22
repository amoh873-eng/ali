using ERPSystem.Application.DTOs.Sales;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

public partial class SalesInvoiceService
{
    public async Task<List<TopSellingItemDto>> GetTopSellingItemsAsync(int count = 5)
    {
        var rows = await _context.Set<Domain.Entities.SalesInvoiceLine>()
            .Where(l => l.SalesInvoice != null && !l.SalesInvoice.IsDeleted)
            .GroupBy(l => new { l.ItemId, NameAr = l.Item!.NameAr })
            .Select(g => new TopSellingItemDto
            {
                ItemId = g.Key.ItemId,
                ItemNameAr = g.Key.NameAr,
                TotalAmount = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(count)
            .ToListAsync();
        return rows;
    }
}
