using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// يبني التقارير المالية (ميزان المراجعة، قائمة الدخل، الميزانية العمومية)
/// من بنود القيود المحاسبية المخزّنة.
/// </summary>
public class ReportService : IReportService
{
    private readonly DbContext _context;

    public ReportService(DbContext context)
    {
        _context = context;
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync()
    {
        var lines = await LoadAllLinesAsync();

        var grouped = lines
            .Where(l => l.Account is not null)
            .GroupBy(l => l.AccountId)
            .Select(g =>
            {
                var account = g.First().Account!;
                var debit = g.Sum(x => x.DebitAmount);
                var credit = g.Sum(x => x.CreditAmount);
                return new TrialBalanceLineDto
                {
                    AccountId = g.Key,
                    AccountCode = account.Code,
                    AccountNameAr = account.NameAr,
                    TotalDebit = Math.Round(debit, 2),
                    TotalCredit = Math.Round(credit, 2),
                    Balance = Math.Round(debit - credit, 2)
                };
            })
            .OrderBy(l => l.AccountCode)
            .ToList();

        return new TrialBalanceDto
        {
            TotalDebit = Math.Round(grouped.Sum(l => l.TotalDebit), 2),
            TotalCredit = Math.Round(grouped.Sum(l => l.TotalCredit), 2),
            Lines = grouped
        };
    }

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(DateTime from, DateTime to)
    {
        var relevant = await _context.Set<JournalEntryLine>()
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry != null
                        && l.JournalEntry.EntryDate >= from.Date
                        && l.JournalEntry.EntryDate < to.Date.AddDays(1))
            .ToListAsync();

        var revenues = AggregateByAccount(relevant, AccountType.Revenue, creditNormal: true);
        var expenses = AggregateByAccount(relevant, AccountType.Expense, creditNormal: false);

        return new IncomeStatementDto
        {
            From = from,
            To = to,
            Revenues = revenues,
            TotalRevenue = Math.Round(revenues.Sum(r => r.Amount), 2),
            Expenses = expenses,
            TotalExpenses = Math.Round(expenses.Sum(e => e.Amount), 2)
        };
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOf)
    {
        var relevant = await _context.Set<JournalEntryLine>()
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry != null
                        && l.JournalEntry.EntryDate < asOf.Date.AddDays(1))
            .ToListAsync();

        var assets = AggregateByAccount(relevant, AccountType.Asset, creditNormal: false);
        var liabilities = AggregateByAccount(relevant, AccountType.Liability, creditNormal: true);
        var equity = AggregateByAccount(relevant, AccountType.Equity, creditNormal: true);

        // صافي الدخل = الإيرادات - المصروفات، يُدرج كأرباح محتجزة ضمن حقوق الملكية.
        var netIncome = SumNet(relevant, AccountType.Revenue, creditNormal: true)
                        - SumNet(relevant, AccountType.Expense, creditNormal: false);

        if (netIncome != 0)
        {
            equity.Add(new StatementLineDto
            {
                AccountCode = string.Empty,
                AccountNameAr = "صافي الدخل (أرباح محتجزة)",
                Amount = Math.Round(netIncome, 2)
            });
        }

        return new BalanceSheetDto
        {
            AsOf = asOf,
            Assets = assets,
            TotalAssets = Math.Round(assets.Sum(a => a.Amount), 2),
            Liabilities = liabilities,
            TotalLiabilities = Math.Round(liabilities.Sum(l => l.Amount), 2),
            Equity = equity,
            TotalEquity = Math.Round(equity.Sum(e => e.Amount), 2)
        };
    }

    public async Task<AccountLedgerDto> GetAccountLedgerAsync(Guid accountId, DateTime from, DateTime to)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException("الحساب غير موجود");

        var creditNormal = account.NormalBalance == NormalBalance.Credit;

        var lines = await _context.Set<JournalEntryLine>()
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId
                        && l.JournalEntry != null
                        && l.JournalEntry.EntryDate < to.Date.AddDays(1))
            .ToListAsync();

        // أثر كل حركة على رصيد الحساب حسب طبيعته: المدين يزيد الحسابات المدينة،
        // والدائن يزيد الحسابات الدائنة.
        var movements = lines
            .Select(l => new
            {
                Date = l.JournalEntry?.EntryDate ?? l.CreatedAt,
                EntryNumber = l.JournalEntry?.EntryNumber ?? "—",
                Description = l.JournalEntry?.Description,
                Debit = l.DebitAmount,
                Credit = l.CreditAmount,
                Effect = creditNormal ? l.CreditAmount - l.DebitAmount : l.DebitAmount - l.CreditAmount
            })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.EntryNumber)
            .ToList();

        var opening = movements.Where(x => x.Date.Date < from.Date).Sum(x => x.Effect);
        var running = opening;
        var ledgerLines = new List<LedgerLineDto>();

        foreach (var x in movements.Where(x => x.Date.Date >= from.Date && x.Date.Date <= to.Date))
        {
            running += x.Effect;
            ledgerLines.Add(new LedgerLineDto
            {
                Date = x.Date,
                EntryNumber = x.EntryNumber,
                Description = x.Description,
                Debit = Math.Round(x.Debit, 2),
                Credit = Math.Round(x.Credit, 2),
                Balance = Math.Round(running, 2)
            });
        }

        return new AccountLedgerDto
        {
            AccountId = account.Id,
            AccountCode = account.Code,
            AccountNameAr = account.NameAr,
            From = from,
            To = to,
            OpeningBalance = Math.Round(opening, 2),
            ClosingBalance = Math.Round(running, 2),
            Lines = ledgerLines
        };
    }

    // ==================== Helpers ====================

    private async Task<List<JournalEntryLine>> LoadAllLinesAsync()
    {
        return await _context.Set<JournalEntryLine>()
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .ToListAsync();
    }

    /// <summary>
    /// يجمع بنود القيد حسب الحساب ويعرض المبلغ بطبيعة الحساب:
    /// الحسابات الدائنة (خصوم/حقوق/إيرادات) موجبة عندما يكون الدائن أكبر،
    /// والحسابات المدينة (أصول/مصروفات) موجبة عندما يكون المدين أكبر.
    /// </summary>
    private static List<StatementLineDto> AggregateByAccount(
        List<JournalEntryLine> lines, AccountType type, bool creditNormal)
    {
        return lines
            .Where(l => l.Account is not null && l.Account.AccountType == type)
            .GroupBy(l => l.AccountId)
            .Select(g =>
            {
                var account = g.First().Account!;
                var net = g.Sum(x => x.DebitAmount) - g.Sum(x => x.CreditAmount);
                var amount = creditNormal ? -net : net;
                return new StatementLineDto
                {
                    AccountCode = account.Code,
                    AccountNameAr = account.NameAr,
                    Amount = Math.Round(amount, 2)
                };
            })
            .Where(l => l.Amount != 0)
            .OrderBy(l => l.AccountCode)
            .ToList();
    }

    private static decimal SumNet(List<JournalEntryLine> lines, AccountType type, bool creditNormal)
    {
        var net = lines
            .Where(l => l.Account is not null && l.Account.AccountType == type)
            .Sum(l => l.DebitAmount - l.CreditAmount);
        return creditNormal ? -net : net;
    }
}