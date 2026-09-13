using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.DTOs.Shifts;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// ورديات الكاشير وتسوية درج الكاش:
///  Expected = OpeningFloat + Σمبيعات نقدية POS − Σمردودات نقدية + ΣFloatIn − ΣCashOut
///  (ضمن نافذة الوردية [StartTime, EndTime) — بمقياس «الدَّرَج»: كل حركة نقد على الدرج
///  في النافذة، لأن فواتير POS تحمل CreatedBy فارغاً ولا يمكن عزل كاشير موثوق لكل فاتورة).
/// القيد عند وجود فرق (ضمن نفس معاملة الإغلاق):
///  عجز  : مدين «فروقات الصندوق» 5110 / دائن «درج الكاش» 1105
///  زيادة: مدين 1105 / دائن 5110
///  صفر  : بلا قيد.
/// </summary>
public class CashierShiftService : ICashierShiftService
{
    private const string AccountMainSafe = "1100";
    private const string AccountTillDrawer = "1105";
    private const string AccountVariance = "5110";

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public CashierShiftService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    public async Task<CashierShiftDto> StartAsync(decimal openingFloatAmount, string cashierUserId)
    {
        if (string.IsNullOrWhiteSpace(cashierUserId))
            throw new InvalidOperationException("يجب تحديد الكاشير.");
        if (openingFloatAmount <= 0m)
            throw new InvalidOperationException("رصيد الفكة الافتتاحي يجب أن يكون أكبر من صفر.");

        var existing = await _context.Set<CashierShift>()
            .Where(s => s.CashierUserId == cashierUserId && s.Status == CashierShiftStatus.Open)
            .FirstOrDefaultAsync();
        if (existing is not null)
            throw new InvalidOperationException("توجد وردية مفتوحة بالفعل لهذا الكاشير — أغلقها قبل فتح وردية جديدة.");

        var safeAccount = await GetAccountByCodeAsync(AccountMainSafe);
        var tillAccount = await GetAccountByCodeAsync(AccountTillDrawer);
        var startTime = DateTime.Now;
        var reference = await NumberSequenceHelper.NextAsync(_context, "SH");

        var shift = new CashierShift
        {
            Id = Guid.NewGuid(),
            CashierUserId = cashierUserId,
            StartTime = startTime,
            Status = CashierShiftStatus.Open,
            OpeningFloatAmount = openingFloatAmount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // الفكة الافتتاحية: تمويل درج الكاش من الخزنة (Float-In) — قيد متوازن
            var entry = await _journalService.PrepareEntryAsync(
                JournalEntryType.CashTransfer,
                startTime,
                reference,
                $"فكة افتتاحية لوردية الكاشير {cashierUserId} {reference}",
                new List<JournalEntryLegInput>
                {
                    new() { AccountId = tillAccount.Id, DebitAmount = openingFloatAmount },
                    new() { AccountId = safeAccount.Id, CreditAmount = openingFloatAmount }
                });

            var openingTransfer = new CashDrawerTransaction
            {
                Id = Guid.NewGuid(),
                Type = CashTransferDirection.FloatIn,
                Amount = openingFloatAmount,
                Timestamp = startTime,
                CashierUserId = cashierUserId,
                Notes = $"فكة افتتاحية وردية {reference}",
                JournalEntryId = entry.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<CashDrawerTransaction>().Add(openingTransfer);
            shift.OpeningTransferId = openingTransfer.Id;

            _context.Set<CashierShift>().Add(shift);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return MapToDto(shift);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
public async Task<CashierShiftDto?> GetOpenShiftAsync(string cashierUserId)
    {
        var shift = await _context.Set<CashierShift>()
            .Include(s => s.JournalEntry)
            .Where(s => s.CashierUserId == cashierUserId && s.Status == CashierShiftStatus.Open)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();
        return shift is null ? null : MapToDto(shift);
    }

    /// <summary>حساب الرصيد المتوقَّع المعزول لوردية (اختبار المعادلة بمعزل قبل الربط بالشاشات).</summary>
    public async Task<ShiftSummaryDto> ComputeSummaryAsync(Guid shiftId)
    {
        var shift = await _context.Set<CashierShift>()
            .FirstOrDefaultAsync(s => s.Id == shiftId);
        if (shift is null)
            throw new InvalidOperationException("الوردية غير موجودة.");

        var from = shift.StartTime;
        var to = shift.EndTime ?? DateTime.Now.AddMilliseconds(1);
        // نطاق «الدَّرَج» لوردية يومية: تُستوعب مصادره النقدية على مقياسين متكاملين —
        //  الفواتير والمردودات بالتاريخ (فواتير POS تحمل تاريخاً يومياً صافياً فقط، لا طابعاً زمنياً)،
        //  والتحويلات بالطابع الزمني للدرج. النطاقان يغطيان أيام الوردية فقط، لا التراكم.
        var invoiceFrom = from.Date;
        var invoiceTo = to.Date.AddDays(1);

        var summary = new ShiftSummaryDto { OpeningFloat = shift.OpeningFloatAmount };

        // 1) مبيعات نقدية نقطة البيع ضمن أيام الوردية (فواتير POS تحمل تاريخاً يومياً صافياً)
        summary.CashSales = await _context.Set<SalesInvoice>()
            .Where(i => i.IsPos
                && i.PaymentMethod == SalesPaymentMethod.Cash
                && i.InvoiceDate >= invoiceFrom && i.InvoiceDate < invoiceTo)
            .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

        // 2) مردودات نقدية مرتبطة بتلك الفواتير ضمن أيام الوردية
        var posInvoiceIds = await _context.Set<SalesInvoice>()
            .Where(i => i.IsPos
                && i.PaymentMethod == SalesPaymentMethod.Cash
                && i.InvoiceDate >= invoiceFrom && i.InvoiceDate < invoiceTo)
            .Select(i => i.Id).ToListAsync();
        if (posInvoiceIds.Count > 0)
        {
            summary.CashRefunds = await _context.Set<SalesReturn>()
                .Where(r => r.ReturnDate >= invoiceFrom && r.ReturnDate < invoiceTo
                    && posInvoiceIds.Contains(r.SalesInvoiceId))
                .SumAsync(r => (decimal?)r.TotalAmount) ?? 0m;
        }

        // 3) الإضافات والسحوبات النقدية على الدرج ضمن النطاق (عدا تحويل الفتح المرتبط)
        var floats = await _context.Set<CashDrawerTransaction>()
            .Where(t => t.Timestamp >= from && t.Timestamp < to
                && t.Id != shift.OpeningTransferId)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync();
        foreach (var f in floats)
        {
            if (f.Type == CashTransferDirection.FloatIn) summary.FloatsIn += f.Amount;
            else summary.FloatsOut += f.Amount;
        }

        summary.Expected = Math.Round(
            summary.OpeningFloat + summary.CashSales - summary.CashRefunds + summary.FloatsIn - summary.FloatsOut, 2);
        return summary;
    }
public async Task<CashierShiftDto> CloseAsync(Guid shiftId, decimal countedCashAmount, string? notes, string closedByUserId)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var shift = await _context.Set<CashierShift>()
                .FirstOrDefaultAsync(s => s.Id == shiftId);
            if (shift is null)
                throw new InvalidOperationException("الوردية غير موجودة.");
            if (shift.Status == CashierShiftStatus.Closed)
                throw new InvalidOperationException("الوردية مغلقة بالفعل.");
            if (countedCashAmount < 0m)
                throw new InvalidOperationException("المبلغ المعدود لا يمكن أن يكون سالباً.");

            shift.EndTime = DateTime.Now;
            var summary = await ComputeSummaryAsync(shift.Id);
            shift.ExpectedCashAmount = summary.Expected;
            shift.CountedCashAmount = countedCashAmount;
            var variance = Math.Round(countedCashAmount - summary.Expected, 2);
            shift.VarianceAmount = variance;

            if (variance != 0m)
            {
                var abs = Math.Abs(variance);
                var tillAccount = await GetAccountByCodeAsync(AccountTillDrawer);
                var varianceAccount = await GetAccountByCodeAsync(AccountVariance);
                var shortage = variance < 0m;
                var reference = await NumberSequenceHelper.NextAsync(_context, "SH");
                var description = shortage
                    ? $"عجز صندوق (تسوية وردية) {reference}"
                    : $"زيادة صندوق (تسوية وردية) {reference}";
                var entry = await _journalService.PrepareEntryAsync(
                    JournalEntryType.ShiftReconciliation,
                    shift.EndTime.Value,
                    reference,
                    description,
                    new List<JournalEntryLegInput>
                    {
                        // عجز: مدين فروقات الصندوق / دائن درج الكاش
                        // زيادة: مدين درج الكاش / دائن فروقات الصندوق
                        new() { AccountId = (shortage ? varianceAccount : tillAccount).Id, DebitAmount = abs },
                        new() { AccountId = (shortage ? tillAccount : varianceAccount).Id, CreditAmount = abs }
                    });
                shift.JournalEntryId = entry.Id;
            }

            shift.Status = CashierShiftStatus.Closed;
            shift.ClosedByUserId = closedByUserId;
            shift.Notes = notes;
            shift.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return MapToDto(shift);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<CashierShiftDto>> GetHistoryAsync(CashierShiftFilterDto filter)
    {
        var query = _context.Set<CashierShift>()
            .Include(s => s.JournalEntry)
            .Where(s => s.Status == CashierShiftStatus.Closed)
            .AsQueryable();

        if (filter.From.HasValue)
            query = query.Where(s => s.StartTime >= filter.From.Value);
        if (filter.To.HasValue)
        {
            var toExclusive = filter.To.Value.Date.AddDays(1);
            query = query.Where(s => s.StartTime < toExclusive);
        }

        var rows = await query
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(filter.Cashier))
        {
            var term = filter.Cashier.Trim();
            rows = rows.Where(s =>
                s.CashierUserId.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return rows.Select(s => MapToDto(s)).ToList();
    }

    private async Task<Account> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي '{code}' غير موجود في شجرة الحسابات.");
        return account;
    }

    private static CashierShiftDto MapToDto(CashierShift s)
        => new()
        {
            Id = s.Id,
            CashierUserId = s.CashierUserId,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Status = (int)s.Status,
            OpeningFloatAmount = s.OpeningFloatAmount,
            OpeningTransferId = s.OpeningTransferId,
            ExpectedCashAmount = s.ExpectedCashAmount,
            CountedCashAmount = s.CountedCashAmount,
            VarianceAmount = s.VarianceAmount,
            ClosedByUserId = s.ClosedByUserId,
            Notes = s.Notes,
            JournalEntryId = s.JournalEntryId,
            EntryNumber = s.JournalEntry?.EntryNumber ?? "—"
        };
}