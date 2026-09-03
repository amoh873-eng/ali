using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// التحليلات التشغيلية (قراءة فقط): ربحية الأصناف/الفئات، البطيء/الخامل،
/// وتصنيف ABC على أساس قيمة المخزون (كمية × تكلفة). لا تعدّل أي بنية أو إيداع.
/// </summary>
public static class OperationalAnalyticsService
{
    // ── 1) ربحية الأصناف/الفئات: هامش المساهمة = (سعر بيع − تكلفة) × كمية ──
    public static async Task<ItemProfitabilityDto> GetItemProfitabilityAsync(DbContext db, DateTime from, DateTime to)
    {
        var fromDate = from.Date;
        var lines = await db.Set<SalesInvoiceLine>()
            .Include(l => l.Item)
            .Include(l => l.SalesInvoice)
            .Where(l => l.SalesInvoice != null
                        && !l.SalesInvoice.IsDeleted
                        && l.SalesInvoice.Status != DocumentStatus.Cancelled
                        && l.SalesInvoice.InvoiceDate >= fromDate
                        && l.SalesInvoice.InvoiceDate < to.Date.AddDays(1))
            .ToListAsync();

        var rows = lines
            .Where(l => l.Item != null)
            .GroupBy(l => l.ItemId)
            .Select(g =>
            {
                var item = g.First().Item!;
                var revenue = g.Sum(x => x.UnitPrice * x.Quantity);
                var cost = g.Sum(x => x.UnitCost * x.Quantity);
                var margin = revenue - cost;
                return new ItemProfitabilityRow
                {
                    ItemId = item.Id,
                    ItemCode = item.Code,
                    ItemNameAr = item.NameAr,
                    CategoryNameAr = item.Category?.NameAr ?? "",
                    QtySold = Math.Round(g.Sum(x => x.Quantity), 2),
                    Revenue = Math.Round(revenue, 2),
                    Cost = Math.Round(cost, 2),
                    Margin = Math.Round(margin, 2),
                    MarginPct = revenue == 0 ? 0 : Math.Round(margin / revenue * 100m, 1)
                };
            })
            .OrderByDescending(r => r.Margin)
            .ToList();

        return new ItemProfitabilityDto { From = fromDate, To = to.Date, Rows = rows };
    }

    // ── 2) البطيء/الخامل: رصيد مخزون &gt; 0 بدون مبيع خلال آخر N يوم ──
    public static async Task<SlowMovingDto> GetSlowMovingAsync(DbContext db, int daysThreshold)
    {
        var threshold = daysThreshold <= 0 ? 90 : daysThreshold;
        var items = await db.Set<Item>()
            .Where(i => !i.IsDeleted && i.CurrentStock > 0)
            .ToListAsync();

        var lastSales = await db.Set<SalesInvoiceLine>()
            .Where(l => l.SalesInvoice != null
                        && !l.SalesInvoice.IsDeleted
                        && l.SalesInvoice.Status != DocumentStatus.Cancelled)
            .GroupBy(l => l.ItemId)
            .Select(g => new { ItemId = g.Key, Last = g.Max(l => l.SalesInvoice!.InvoiceDate) })
            .ToListAsync();
        var lastSaleByItem = lastSales.ToDictionary(x => x.ItemId, x => x.Last);

        var today = System.DateTime.Today;
        var rows = new List<SlowMovingRow>();
        foreach (var item in items)
        {
            var last = lastSaleByItem.TryGetValue(item.Id, out var d) ? d : (DateTime?)null;
            if (last.HasValue && (today - last.Value.Date).Days < threshold) continue;

            rows.Add(new SlowMovingRow
            {
                ItemId = item.Id,
                ItemCode = item.Code,
                ItemNameAr = item.NameAr,
                CurrentStock = item.CurrentStock,
                StockValue = Math.Round(item.CurrentStock * item.CostPrice, 2),
                NeverSold = !last.HasValue,
                DaysSinceLastSale = last.HasValue ? (today - last.Value.Date).Days : (int?)null
            });
        }

        return new SlowMovingDto
        {
            DaysThreshold = threshold,
            Rows = rows.OrderByDescending(r => r.StockValue).ToList()
        };
    }

    // ── 3) تصنيف ABC على أساس قيمة المخزون: A≈80% تراكمي، B≈15%، C≈5% ──
    public static async Task<AbcAnalysisDto> GetAbcAnalysisAsync(DbContext db)
    {
        var items = await db.Set<Item>()
            .Where(i => !i.IsDeleted && i.CurrentStock > 0)
            .ToListAsync();

        var total = items.Sum(i => i.CurrentStock * i.CostPrice);
        if (total <= 0) return new AbcAnalysisDto { TotalValue = 0 };

        decimal cumulative = 0;
        var rows = new List<AbcAnalysisRow>();
        foreach (var item in items.OrderByDescending(i => i.CurrentStock * i.CostPrice))
        {
            var value = item.CurrentStock * item.CostPrice;
            cumulative += value / total * 100m;
            rows.Add(new AbcAnalysisRow
            {
                ItemId = item.Id,
                ItemCode = item.Code,
                ItemNameAr = item.NameAr,
                Value = Math.Round(value, 2),
                SharePct = Math.Round(value / total * 100m, 1),
                CumulativePct = Math.Round(cumulative, 1),
                Tier = cumulative <= 80m ? "A" : cumulative <= 95m ? "B" : "C"
            });
        }

        return new AbcAnalysisDto { TotalValue = Math.Round(total, 2), Rows = rows };
    }
}