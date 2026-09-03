using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// خدمة المؤشرات المالية — قراءة فقط. تحسب مؤشرات السيولة والربحية والكفاءة
/// للفترة [from,to] وتقارنها بالفترة السابقة المماثلة.
/// مصادر البيانات:
///   - الأرصدة (نهاية/بداية الفترة) عبر TrialBalanceAsOfHelper (ميزان مراجعة حتى تاريخ).
///   - الإيرادات/المصروفات عبر البنود نفسها (نمط GetIncomeStatementAsync).
///   - متوسط الأرصدة = (رصيد بداية الفترة + رصيد نهاية الفترة) / 2.
/// </summary>
public static class FinancialRatiosService
{
    // ── تصنيف الحسابات حسب بادئة الكود (متوافق مع شجرة الحسابات الحالية) ──
    private const string AssetsCurrentPref = "11";    // نقدية/بنك
    private const string ReceivablesPref = "12";      // عملاء/مدينون
    private const string InventoryPref = "13";        // مخزون
    private const string LiabilitiesCurrentPref = "2";// خصوم متداولة (21/22/23…)
    private const string PayablesPref = "22";         // موردون/دائنون
    private const int DaysInYear = 365;

    public static async Task<FinancialRatiosDto> ComputeAsync(DbContext db, DateTime from, DateTime to)
    {
        var fromDate = from.Date;
        var toDate = to.Date;

        // نطاق الفترة السابقة = نفس طول الفترة الحالية باتجاه الماضي
        var periodLength = toDate - fromDate;
        var prevTo = fromDate.AddDays(-1);
        var prevFrom = prevTo.AddDays(-periodLength.TotalDays);

        // 1) ميزان بداية/نهاية الفترة (والفترة السابقة) لكل حساب
        var balEnd = await TrialBalanceAsOfHelper.BalancesAsOfAsync(db, toDate);
        var balStart = await TrialBalanceAsOfHelper.BalancesAsOfAsync(db, fromDate.AddDays(-1));
        var balPrevEnd = await TrialBalanceAsOfHelper.BalancesAsOfAsync(db, prevTo);
        var balPrevStart = await TrialBalanceAsOfHelper.BalancesAsOfAsync(db, prevFrom.AddDays(-1));

        // 2) قائمة الحسابات حسب النوع (لتصنيف المتداول ببادئة الكود)
        var accounts = await db.Set<Account>().Where(a => !a.IsDeleted).ToListAsync();
        var currentAssets = accounts.Where(a => a.AccountType == AccountType.Asset
                && a.Code.StartsWith(AssetsCurrentPref)
                && !a.Code.StartsWith(ReceivablesPref) && !a.Code.StartsWith(InventoryPref))
            .Select(a => a.Id).ToList();
        var receivables = accounts.Where(a => a.Code.StartsWith(ReceivablesPref)).Select(a => a.Id).ToList();
        var inventory = accounts.Where(a => a.Code.StartsWith(InventoryPref)).Select(a => a.Id).ToList();
        var currentLiabilities = accounts.Where(a => a.AccountType == AccountType.Liability
                && a.Code.StartsWith(LiabilitiesCurrentPref))
            .Select(a => a.Id).ToList();
        var payables = accounts.Where(a => a.Code.StartsWith(PayablesPref)).Select(a => a.Id).ToList();

        // 3) الإيرادات والمصروفات داخل الفترة (نمط GetIncomeStatementAsync)
        var (revenue, expenses) = await ComputeIncomeAsync(db, fromDate, toDate);
        var (revPrev, expPrev) = await ComputeIncomeAsync(db, prevFrom, prevTo);

        var netIncome = revenue - expenses;
        var netIncomePrev = revPrev - expPrev;
        var cogs = expenses;

        // 4) متوسطات الأرصدة (بداية+نهاية)/2
        var avgCurrentAssets = Avg(balStart, balEnd, currentAssets);
        var avgCurrentLiabilities = Avg(balStart, balEnd, currentLiabilities);
        var avgInventory = Avg(balStart, balEnd, inventory);
        var avgReceivables = Avg(balStart, balEnd, receivables);
        var avgPayables = Avg(balStart, balEnd, payables);

        var avgCurrentAssetsPrev = Avg(balPrevStart, balPrevEnd, currentAssets);
        var avgCurrentLiabilitiesPrev = Avg(balPrevStart, balPrevEnd, currentLiabilities);
        var avgInventoryPrev = Avg(balPrevStart, balPrevEnd, inventory);
        var avgReceivablesPrev = Avg(balPrevStart, balPrevEnd, receivables);
        var avgPayablesPrev = Avg(balPrevStart, balPrevEnd, payables);
        // __NEXT__
        var totalAssetsEnd = TrialBalanceAsOfHelper.SumOf(balEnd, accounts.Where(a => a.AccountType == AccountType.Asset).Select(a => a.Id));
        var totalAssetsPrevEnd = TrialBalanceAsOfHelper.SumOf(balPrevEnd, accounts.Where(a => a.AccountType == AccountType.Asset).Select(a => a.Id));

        // 5) المؤشرات
        var cats = new List<RatioCategory>();

        // السيولة
        var currentRatio = SafeDiv(avgCurrentAssets, avgCurrentLiabilities);
        var quickRatio = SafeDiv(avgCurrentAssets - avgInventory, avgCurrentLiabilities);
        var currentRatioPrev = SafeDiv(avgCurrentAssetsPrev, avgCurrentLiabilitiesPrev);
        var quickRatioPrev = SafeDiv(avgCurrentAssetsPrev - avgInventoryPrev, avgCurrentLiabilitiesPrev);
        cats.Add(new RatioCategory
        {
            Key = "liquidity",
            Items = new List<FinancialRatioItem>
            {
                new() { Key = "currentRatio", CurrentValue = currentRatio, PreviousValue = currentRatioPrev, IsPositiveMove = currentRatio >= currentRatioPrev },
                new() { Key = "quickRatio", CurrentValue = quickRatio, PreviousValue = quickRatioPrev, IsPositiveMove = quickRatio >= quickRatioPrev }
            }
        });

        // الربحية
        var gross = SafeDiv(revenue - cogs, revenue);
        var net = SafeDiv(netIncome, revenue);
        var roa = SafeDiv(netIncome, totalAssetsEnd);
        var grossPrev = SafeDiv(revPrev - expPrev, revPrev);
        var netPrev = SafeDiv(netIncomePrev, revPrev);
        var roaPrev = SafeDiv(netIncomePrev, totalAssetsPrevEnd);
        cats.Add(new RatioCategory
        {
            Key = "profitability",
            Items = new List<FinancialRatioItem>
            {
                new() { Key = "grossMargin", CurrentValue = gross, PreviousValue = grossPrev, IsPositiveMove = gross >= grossPrev },
                new() { Key = "netMargin", CurrentValue = net, PreviousValue = netPrev, IsPositiveMove = net >= netPrev },
                new() { Key = "roa", CurrentValue = roa, PreviousValue = roaPrev, IsPositiveMove = roa >= roaPrev }
            }
        });
        // __NEXT2__
        // الكفاءة التشغيلية
        var invTurnover = SafeDiv(cogs, avgInventory);
        var invTurnoverPrev = SafeDiv(expPrev, avgInventoryPrev);
        var arTurnover = SafeDiv(revenue, avgReceivables);
        var arTurnoverPrev = SafeDiv(revPrev, avgReceivablesPrev);
        var dso = SafeDiv(DaysInYear * avgReceivables, revenue);
        var dsoPrev = SafeDiv(DaysInYear * avgReceivablesPrev, revPrev);

        var purchases = await SumPurchasesAsync(db, fromDate, toDate);
        var purchasesPrev = await SumPurchasesAsync(db, prevFrom, prevTo);
        var dpo = SafeDiv(DaysInYear * avgPayables, purchases);
        var dpoPrev = SafeDiv(DaysInYear * avgPayablesPrev, purchasesPrev);

        var dio = SafeDiv(DaysInYear, invTurnover);
        var dioPrev = SafeDiv(DaysInYear, invTurnoverPrev);
        var ccc = dio.HasValue && dso.HasValue && dpo.HasValue ? dio.Value + dso.Value - dpo.Value : (decimal?)null;
        var cccPrev = dioPrev.HasValue && dsoPrev.HasValue && dpoPrev.HasValue ? dioPrev.Value + dsoPrev.Value - dpoPrev.Value : (decimal?)null;

        cats.Add(new RatioCategory
        {
            Key = "efficiency",
            Items = new List<FinancialRatioItem>
            {
                new() { Key = "inventoryTurnover", CurrentValue = invTurnover, PreviousValue = invTurnoverPrev, IsPositiveMove = invTurnover >= invTurnoverPrev },
                new() { Key = "arTurnover", CurrentValue = arTurnover, PreviousValue = arTurnoverPrev, IsPositiveMove = arTurnover >= arTurnoverPrev },
                new() { Key = "dso", CurrentValue = dso, PreviousValue = dsoPrev, IsPositiveMove = dso <= dsoPrev },
                new() { Key = "dpo", CurrentValue = dpo, PreviousValue = dpoPrev, IsPositiveMove = dpo <= dpoPrev },
                new() { Key = "ccc", CurrentValue = ccc.HasValue ? Math.Round(ccc.Value, 1) : null, PreviousValue = cccPrev.HasValue ? Math.Round(cccPrev.Value, 1) : null, IsPositiveMove = ccc <= cccPrev }
            }
        });

        return new FinancialRatiosDto { From = fromDate, To = toDate, Categories = cats };
    }

    private static decimal? Avg(Dictionary<Guid, decimal> s, Dictionary<Guid, decimal> e, List<Guid> ids)
    {
        var sum = TrialBalanceAsOfHelper.SumOf(s, ids) + TrialBalanceAsOfHelper.SumOf(e, ids);
        return sum == 0 ? null : sum / 2;
    }

    private static decimal? SafeDiv(decimal? a, decimal? b)
        => !a.HasValue || !b.HasValue || b.Value == 0 ? null : Math.Round(a.Value / b.Value, 3);

    private static async Task<(decimal Revenue, decimal Expenses)> ComputeIncomeAsync(DbContext db, DateTime from, DateTime to)
    {
        var lines = await db.Set<JournalEntryLine>()
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry != null
                        && l.JournalEntry.EntryDate >= from
                        && l.JournalEntry.EntryDate < to.AddDays(1))
            .ToListAsync();
        var revenue = lines
            .Where(l => l.Account is not null && l.Account.AccountType == AccountType.Revenue)
            .Sum(l => l.CreditAmount - l.DebitAmount);
        var expenses = lines
            .Where(l => l.Account is not null && l.Account.AccountType == AccountType.Expense)
            .Sum(l => l.DebitAmount - l.CreditAmount);
        return (Math.Round(revenue, 2), Math.Round(expenses, 2));
    }

    private static async Task<decimal> SumPurchasesAsync(DbContext db, DateTime from, DateTime to)
    {
        var sum = await db.Set<PurchaseInvoice>()
            .Where(p => !p.IsDeleted && p.Status != DocumentStatus.Cancelled
                        && p.InvoiceDate >= from && p.InvoiceDate < to.AddDays(1))
            .SumAsync(p => (decimal?)(p.TotalAmount - p.PaidAmount) ?? 0m);
        return sum;
    }
}