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
public partial class ReportService : IReportService
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

    public async Task<BillingStageReportDto> GetBillingStageReportAsync(DateTime from, DateTime to)
    {
        var invoices = await _context.Set<SalesInvoice>()
            .Where(s => s.InvoiceDate >= from.Date && s.InvoiceDate < to.Date.AddDays(1) && !s.IsDeleted)
            .OrderBy(s => s.InvoiceDate).ThenBy(s => s.InvoiceNumber)
            .ToListAsync();
        var list = invoices.Select(s => new BillingStageInvoiceDto
        {
            InvoiceNumber = s.InvoiceNumber,
            InvoiceDate = s.InvoiceDate,
            TotalAmount = s.TotalAmount,
            JoFotaraStatus = (int)s.JoFotaraStatus
        }).ToList();
        return new BillingStageReportDto
        {
            From = from,
            To = to,
            Invoices = list,
            TotalAmount = list.Sum(x => x.TotalAmount),
            SubmittedCount = list.Count(x => x.JoFotaraStatus == 1),
            FailedCount = list.Count(x => x.JoFotaraStatus == 2),
            NotSubmittedCount = list.Count(x => x.JoFotaraStatus == 0)
        };
    }
    public byte[] ExportBillingStageExcel(BillingStageReportDto dto)
    {
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("BillingStage");
        ws.Cell(1, 1).Value = $"من {dto.From:yyyy-MM-dd} إلى {dto.To:yyyy-MM-dd}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = "رقم الفاتورة"; ws.Cell(2, 2).Value = "التاريخ"; ws.Cell(2, 3).Value = "الإجمالي"; ws.Cell(2, 4).Value = "JoFotara";
        ws.Row(2).Style.Font.Bold = true;
        int r = 3;
        foreach (var inv in dto.Invoices)
        {
            ws.Cell(r, 1).Value = inv.InvoiceNumber;
            ws.Cell(r, 2).Value = inv.InvoiceDate.ToString("yyyy-MM-dd");
            ws.Cell(r, 3).Value = inv.TotalAmount; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r, 4).Value = inv.JoFotaraStatus == 1 ? "مُرحل" : inv.JoFotaraStatus == 2 ? "فشل" : "غير مُرحل";
            r++;
        }
        ws.Cell(r, 1).Value = "الإجمالي"; ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Cell(r, 3).Value = dto.TotalAmount; ws.Cell(r, 3).Style.Font.Bold = true; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(r + 1, 1).Value = $"مُرحل: {dto.SubmittedCount} | فشل: {dto.FailedCount} | غير مُرحل: {dto.NotSubmittedCount}";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }
}