using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// قائمة التدفق النقدي (بصيغة غير مباشرة) — قراءة فقط من بنود القيود المحاسبية.
/// التصنيف بنفس بادئات أكواد الحسابات المعتمدة في المؤشرات المالية:
///   نقدية/بنك = "11"، ذمم مدينة = "12"، مخزون = "13"، ذمم دائنة = "22"،
///   خصوم متداولة أخرى = "2" عدا "22".
/// التغيّرات المحسوبة: الفرق بين رصيد نهاية الفترة ورصيد بدايتها
/// (عبر TrialBalanceAsOfHelper — نمط رصيد حتى تاريخ).
/// </summary>
public static class CashFlowService
{
    private const string CashPref = "11";
    private const string ArPref = "12";
    private const string InventoryPref = "13";
    private const string ApPref = "22";
    private const string LiabilitiesPref = "2";

    public static async Task<CashFlowStatementDto> ComputeAsync(DbContext db, DateTime from, DateTime to)
    {
        var fromDate = from.Date;
        var toDate = to.Date;

        var balStart = await TrialBalanceAsOfHelper.BalancesAsOfAsync(db, fromDate.AddDays(-1));
        var balEnd = await TrialBalanceAsOfHelper.BalancesAsOfAsync(db, toDate);

        var accounts = await db.Set<Account>().Where(a => !a.IsDeleted).ToListAsync();

        decimal Sum(Dictionary<Guid, decimal> bal, Func<Account, bool> match)
            => accounts.Where(match).Sum(a => TrialBalanceAsOfHelper.Value(bal, a.Id));

        var isCash = (Account a) => a.Code.StartsWith(CashPref);
        var isAr = (Account a) => a.Code.StartsWith(ArPref);
        var isInv = (Account a) => a.Code.StartsWith(InventoryPref);
        var isAp = (Account a) => a.Code.StartsWith(ApPref);
        var isOtherLiab = (Account a) => a.AccountType == AccountType.Liability
                                       && a.Code.StartsWith(LiabilitiesPref)
                                       && !a.Code.StartsWith(ApPref);

        // صافي الدخل = إيرادات − مصروفات داخل الفترة (نمط GetIncomeStatementAsync)
        var (revenue, expenses) = await ComputeIncomeAsync(db, fromDate, toDate);
        var netIncome = revenue - expenses;

        // تغيّرات رأس المال العامل (نهاية − بداية)
        var arDelta = Sum(balEnd, isAr) - Sum(balStart, isAr);
        var invDelta = Sum(balEnd, isInv) - Sum(balStart, isInv);
        var apDelta = Sum(balEnd, isAp) - Sum(balStart, isAp);
        var otherLiabDelta = Sum(balEnd, isOtherLiab) - Sum(balStart, isOtherLiab);

        // أقسام التدفق
        var operating = new CashFlowSection
        {
            Key = "operating",
            TitleKey = "CashFlowOperating",
            Lines = new List<CashFlowLine>
            {
                new() { DescriptionKey = "CashFlowNetIncome", Amount = Math.Round(netIncome, 2) },
                new() { DescriptionKey = "CashFlowAdjAR", Amount = Math.Round(-arDelta, 2) },
                new() { DescriptionKey = "CashFlowAdjInv", Amount = Math.Round(-invDelta, 2) },
                new() { DescriptionKey = "CashFlowAdjAP", Amount = Math.Round(apDelta, 2) },
                new() { DescriptionKey = "CashFlowAdjOtherLiab", Amount = Math.Round(otherLiabDelta, 2) }
            }
        };
        operating.Subtotal = Math.Round(operating.Lines.Sum(l => l.Amount), 2);

        var investing = new CashFlowSection { Key = "investing", TitleKey = "CashFlowInvesting" };
        var financing = new CashFlowSection { Key = "financing", TitleKey = "CashFlowFinancing" };

        var netChange = operating.Subtotal + investing.Subtotal + financing.Subtotal;
        var beginningCash = Math.Round(Sum(balStart, isCash), 2);
        var endingCash = Math.Round(beginningCash + netChange, 2);
        var glEndingCash = Math.Round(Sum(balEnd, isCash), 2);

        return new CashFlowStatementDto
        {
            From = fromDate,
            To = toDate,
            Sections = new List<CashFlowSection> { operating, investing, financing },
            NetChangeCash = Math.Round(netChange, 2),
            BeginningCash = beginningCash,
            EndingCash = endingCash,
            GlEndingCash = glEndingCash
        };
    }

    private static async Task<(decimal Revenue, decimal Expenses)> ComputeIncomeAsync(DbContext db, DateTime from, DateTime to)
    {
        var lines = await db.Set<JournalEntryLine>()
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry != null
                        && l.JournalEntry.EntryDate >= from
                        && l.JournalEntry.EntryDate < to.AddDays(1))
            .ToListAsync();
        var revenue = lines.Where(l => l.Account is not null && l.Account.AccountType == AccountType.Revenue)
            .Sum(l => l.CreditAmount - l.DebitAmount);
        var expenses = lines.Where(l => l.Account is not null && l.Account.AccountType == AccountType.Expense)
            .Sum(l => l.DebitAmount - l.CreditAmount);
        return (Math.Round(revenue, 2), Math.Round(expenses, 2));
    }
}