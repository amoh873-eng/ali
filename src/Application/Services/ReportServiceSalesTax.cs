using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

public partial class ReportService
{
    /// <summary>
    /// توليد تقرير ضريبة المبيعات: يجمع فواتير المبيعات في [from, to] حسب نسبة
    /// الضريبة (شريحة ضريبية) ويعيد لكل شريحة عدد الفواتير وإجمالي المبيعات
    /// ومبلغ الضريبة، مع إجمالي مدين/دائن من القيود المحاسبية المرتبطة.
    ///
    /// الاستعلام مصمم للكفاءة:
    ///   - الفلترة (فترة + عدم الحذف + غير ملغاة) تُنفَّذ في قاعدة البيانات
    ///     عبر IQueryable قبل السحب إلى الذاكرة (ToListAsync).
    ///   - للتجميع حسب النسبة تُسحب الفواتير لفترة واحدة فقط مرة واحدة ثم تُجمَّع
    ///     في الذاكرة (عدد الفواتير في فترة واحدة صغير نسبياً، والتجميع بسيط).
    ///   - قيود البيع تُقرأ بجملة IN واحدة (Contains) بدل استعلام لكل فاتورة.
    /// </summary>
    public async Task<SalesTaxReportDto> GetSalesTaxReportAsync(DateTime from, DateTime to)
    {
        var fromDate = from.Date;
        var toExclusive = to.Date.AddDays(1);

        // 1) فواتير الفترة (IQueryable → فلترة في DB)
        var invoices = await _context.Set<SalesInvoice>()
            .Include(x => x.Customer)
            .Where(s => !s.IsDeleted
                        && s.Status != DocumentStatus.Cancelled
                        && s.InvoiceDate >= fromDate
                        && s.InvoiceDate < toExclusive)
            .OrderBy(s => s.InvoiceDate)
            .ThenBy(s => s.InvoiceNumber)
            .ToListAsync();

        // 2) تجميع الفواتير حسب نسبة الضريبة
        var rows = invoices
            .GroupBy(s => Math.Round(s.TaxRate, 2))
            .Select(g => new SalesTaxReportRow
            {
                TaxRate = g.Key,
                TaxDescription = SalesTaxTierCatalog.Describe(g.Key),
                InvoiceCount = g.Count(),
                GrossAmount = Math.Round(g.Sum(x => x.SubTotal - x.DiscountAmount), 2),
                TaxAmount = Math.Round(g.Sum(x => x.TaxAmount), 2)
            })
            .OrderByDescending(r => r.TaxRate)
            .ToList();

        // 3) إجمالي المدين والدائن من القيود المحاسبية المرتبطة بفواتير البيع (صندوق التحقق في التذييل)
        var entryIds = invoices
            .Select(s => s.SalesJournalEntryId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        decimal totalDebit = 0m, totalCredit = 0m;
        if (entryIds.Count > 0)
        {
            var lines = await _context.Set<JournalEntryLine>()
                .Where(l => entryIds.Contains(l.JournalEntryId))
                .ToListAsync();
            totalDebit = Math.Round(lines.Sum(l => l.DebitAmount), 2);
            totalCredit = Math.Round(lines.Sum(l => l.CreditAmount), 2);
        }

        return new SalesTaxReportDto
        {
            From = from,
            To = to,
            Rows = rows,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit
        };
    }
}

/// <summary>
/// كتالوج شرائح الضريبة: يصف نسبة الضريبة باسم عربي موحد (وصف الضريبة المستحق).
/// النسبة 0% تُعرض كـ "معفاة / نسبة صفرية". النسب غير المعروفة تُولّد وصفاً ديناميكياً.
/// </summary>
public static class SalesTaxTierCatalog
{
    private static readonly (decimal Rate, string Description)[] Tiers =
    {
        (16m, "ضريبة مبيعات 16% مستحقة"),
        (10m, "ضريبة مبيعات 10% مستحقة"),
        (5m,  "ضريبة مبيعات 5% مستحقة"),
        (4m,  "ضريبة مبيعات 4% مستحقة"),
        (2m,  "ضريبة مبيعات 2% مستحقة"),
        (0m,  "معفاة من الضريبة / نسبة صفرية"),
    };

    /// <summary>يعيد الوصف الموحد لشريحة الضريبة (بالعربية — يُترجم في الواجهة عند الحاجة).</summary>
    public static string Describe(decimal taxRate)
    {
        foreach (var tier in Tiers)
            if (Math.Abs(tier.Rate - taxRate) < 0.005m)
                return tier.Description;

        return $"ضريبة مبيعات {taxRate:0.#}%";
    }
}