using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERPSystem.Application.Services;

/// <summary>
/// ملف تقارير تحليلية للذمم والمؤشرات المالية — إضافة فقط، لا تعدل أي خدمة إيداع.
/// </summary>
public static class AgingReportQueries
{
    /// <summary>
    /// يجمع الفواتير المفتوحة (Total - Paid &gt; 0) حسب الطرف (عميل/مورد)
    /// ويوزّعها على فئات عمرية مبنية على تاريخ الاستحقاق (DueDate ?? InvoiceDate).
    /// IsReceivable: AR على SalesInvoice، و AP على PurchaseInvoice.
    /// </summary>
    public static async Task<AgingReportDto> GetAgingAsync(DbContext db, bool isReceivable, DateTime asOf)
    {
        var dto = new AgingReportDto { AsOf = asOf.Date, IsReceivable = isReceivable };
        var parties = new List<AgingPartyRow>();
        var asOfDate = asOf.Date;

        if (isReceivable)
        {
            var invs = await db.Set<SalesInvoice>()
                .Where(s => !s.IsDeleted && s.Status != DocumentStatus.Cancelled)
                .Include(s => s.Customer)
                .ToListAsync();

            foreach (var s in invs)
            {
                var bal = s.TotalAmount - s.PaidAmount;
                if (bal <= 0.01m) continue;
                var due = (s.DueDate ?? s.InvoiceDate).Date;
                var days = (asOfDate - due).Days;
                var line = new AgingInvoiceLine { InvoiceNumber = s.InvoiceNumber, InvoiceDate = s.InvoiceDate, DueDate = due, DaysOverdue = days, Balance = bal };

                var row = parties.FirstOrDefault(p => p.PartyId == s.CustomerId);
                if (row is null)
                {
                    row = new AgingPartyRow { PartyId = s.CustomerId, PartyCode = s.Customer?.Code ?? "", PartyNameAr = s.Customer?.NameAr ?? "زبون نقدي" };
                    parties.Add(row);
                }
                AddBucket(row, days, bal);
                row.Invoices.Add(line);
            }
        }
        else
        {
            var invs = await db.Set<PurchaseInvoice>()
                .Where(s => !s.IsDeleted && s.Status != DocumentStatus.Cancelled)
                .Include(s => s.Supplier)
                .ToListAsync();

            foreach (var s in invs)
            {
                var bal = s.TotalAmount - s.PaidAmount;
                if (bal <= 0.01m) continue;
                var due = (s.DueDate ?? s.InvoiceDate).Date;
                var days = (asOfDate - due).Days;
                var line = new AgingInvoiceLine { InvoiceNumber = s.InvoiceNumber, InvoiceDate = s.InvoiceDate, DueDate = due, DaysOverdue = days, Balance = bal };

                var row = parties.FirstOrDefault(p => p.PartyId == s.SupplierId);
                if (row is null)
                {
                    row = new AgingPartyRow { PartyId = s.SupplierId, PartyCode = s.Supplier?.Code ?? "", PartyNameAr = s.Supplier?.NameAr ?? "مورد" };
                    parties.Add(row);
                }
                AddBucket(row, days, bal);
                row.Invoices.Add(line);
            }
        }

        // ترتيب تنازلي بالإجمالي
        dto.Rows = parties.OrderByDescending(p => p.Total).ToList();
        dto.Total0_30 = Math.Round(parties.Sum(p => p.Bucket0_30), 2);
        dto.Total31_60 = Math.Round(parties.Sum(p => p.Bucket31_60), 2);
        dto.Total61_90 = Math.Round(parties.Sum(p => p.Bucket61_90), 2);
        dto.Total90Plus = Math.Round(parties.Sum(p => p.Bucket90Plus), 2);
        return dto;
    }

    private static void AddBucket(AgingPartyRow row, int days, decimal bal)
    {
        if (days <= 30) row.Bucket0_30 += bal;
        else if (days <= 60) row.Bucket31_60 += bal;
        else if (days <= 90) row.Bucket61_90 += bal;
        else row.Bucket90Plus += bal;
    }

    /// <summary>
    /// أعلى N من العملاء حسب إجمالي صافي المبيعات (فواتير غير ملغاة حتى asOf)
    /// مع نسبة كل منهم من إجمالي المبيعات، ويعيد أيضاً ما إذا كان أعلى 5
    /// يتجاوزون 70% من الإجمالي (خطر تركيز مرتفع).
    /// </summary>
    public static async Task<(List<TopCustomerRow> Top, bool Warning)> GetTopCustomersAsync(
        DbContext db, DateTime asOf, int topN = 5, decimal warningThresholdPct = 70m)
    {
        var invoices = await db.Set<SalesInvoice>()
            .Where(s => !s.IsDeleted && s.Status != DocumentStatus.Cancelled
                        && s.InvoiceDate < asOf.Date.AddDays(1))
            .Include(s => s.Customer)
            .ToListAsync();

        var perCustomer = invoices
            .GroupBy(s => new { Id = s.CustomerId, Name = s.Customer?.NameAr ?? "زبون نقدي" })
            .Select(g => new TopCustomerRow
            {
                Name = g.Key.Name,
                Amount = Math.Round(g.Sum(x => x.TotalAmount), 2)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalSales = perCustomer.Sum(x => x.Amount);
        foreach (var r in perCustomer)
            r.Percent = totalSales > 0 ? Math.Round(r.Amount / totalSales * 100m, 1) : 0m;

        var top = perCustomer.Take(topN).ToList();
        var topShare = top.Sum(x => x.Percent);
        return (top, topShare > warningThresholdPct);
    }

    /// <summary>صف بسيط لأعلى العملاء (إجمالي + نسبة).</summary>
    public sealed class TopCustomerRow
    {
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Percent { get; set; }
    }
}

/// <summary>
/// مساعد ميزان المراجعة حتى تاريخ محدد (بداية/نهاية الفترة) — بنفس نمط
/// GetBalanceSheetAsync/GetAccountLedgerAsync: يجمع حركات JournalEntryLine قبل
/// تاريخ محدد ثم يجمعها لكل حساب.
/// </summary>
public static class TrialBalanceAsOfHelper
{
    public static async Task<Dictionary<Guid, decimal>> BalancesAsOfAsync(DbContext db, DateTime asOf)
    {
        var lines = await db.Set<JournalEntryLine>()
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry != null && l.JournalEntry.EntryDate < asOf.Date.AddDays(1))
            .ToListAsync();

        var dict = new Dictionary<Guid, decimal>();
        foreach (var l in lines)
        {
            if (l.Account is null) continue;
            var effect = l.Account.NormalBalance == NormalBalance.Credit
                ? l.CreditAmount - l.DebitAmount
                : l.DebitAmount - l.CreditAmount;
            dict.TryGetValue(l.AccountId, out var cur);
            dict[l.AccountId] = cur + effect;
        }
        return dict;
    }

    /// <summary>يعيد قيمة حساب في ميزان تاريخ محدد (0 إن لم يوجد).</summary>
    public static decimal Value(Dictionary<Guid, decimal> bal, Guid accountId)
        => bal.TryGetValue(accountId, out var v) ? v : 0m;

    /// <summary>
    /// يجمع أرصدة عدة حسابات (مثل كل الأصول المتداولة) من ميزان تاريخ محدد.
    /// يتجاوز الحسابات الأب (التي لا سطر حركات لها) ويستخدم الحسابات النهائية.
    /// </summary>
    public static decimal SumOf(Dictionary<Guid, decimal> bal, IEnumerable<Guid> accountIds)
        => Math.Round(accountIds.Sum(id => Value(bal, id)), 2);
}