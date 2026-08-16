using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements journal entry (القيد المحاسبي) logic — the heart of double-entry accounting.
///
/// لماذا يعتبر هذا الجزء حاسماً؟
/// كل عملية تجارية يجب أن تتحول إلى قيد متوازن (المدين = الدائن) لضمان توازن
/// الميزانية دائماً. الترحيل هنا "يُجهّز" القيد في السياق المشترك دون حفظ فوري،
/// بحيث يحفظه المتصل (مثل خدمة فاتورة البيع) في نفس SaveChanges مع مستنده ذاته —
/// وهذا يمنحنا الذرية (Atomicity): إما أن يُحفظ المستند مع قيوده معاً، أو لا يُحفظ شيء.
/// </summary>
public partial class JournalEntryService : IJournalEntryService
{
    private readonly DbContext _context;

    public JournalEntryService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<JournalEntryDto>> GetEntriesAsync()
    {
        var entries = await _context.Set<JournalEntry>()
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();

        return entries.Select(MapToDto).ToList();
    }

    public async Task<JournalEntryDto?> GetByIdAsync(Guid id)
    {
        var entry = await _context.Set<JournalEntry>()
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        return entry is null ? null : MapToDto(entry);
    }

    public async Task<List<JournalEntryDto>> GetByAccountAsync(Guid accountId)
    {
        var entries = await _context.Set<JournalEntry>()
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .Where(e => e.Lines.Any(l => l.AccountId == accountId))
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();

        return entries.Select(MapToDto).ToList();
    }

    public async Task<JournalEntry> PrepareEntryAsync(
        JournalEntryType entryType,
        DateTime entryDate,
        string? reference,
        string? description,
        IReadOnlyCollection<JournalEntryLegInput> legs)
    {
        if (legs is null || legs.Count == 0)
            throw new InvalidOperationException("لا يمكن إنشاء قيد بدون بنود.");

        // أهم تحقق محاسبي: توازن القيد
        var totalDebit = legs.Sum(l => l.DebitAmount);
        var totalCredit = legs.Sum(l => l.CreditAmount);
        if (totalDebit != totalCredit)
            throw new InvalidOperationException(
                $"القيد غير متوازن: مجموع المدين ({totalDebit:N2}) ≠ مجموع الدائن ({totalCredit:N2}). لا يمكن ترحيله.");

        // نجلب الحسابات دفعة واحدة لنبني قاموساً سريعاً ولتحديث أرصدتها
        var accountIds = legs.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _context.Set<Account>()
            .Where(a => accountIds.Contains(a.Id))
            .ToListAsync();

        var accountDict = accounts.ToDictionary(a => a.Id);

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNumber = await NextNumberAsync("JE"),
            EntryType = entryType,
            EntryDate = entryDate,
            ReferenceNumber = reference,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var leg in legs)
        {
            if (!accountDict.TryGetValue(leg.AccountId, out var account))
                throw new InvalidOperationException("أحد الحسابات في القيد غير موجود.");

            if (leg.DebitAmount < 0 || leg.CreditAmount < 0)
                throw new InvalidOperationException("لا يمكن أن تكون مبالغ القيد سالبة.");

            var line = new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                JournalEntryId = entry.Id,
                AccountId = leg.AccountId,
                DebitAmount = leg.DebitAmount,
                CreditAmount = leg.CreditAmount,
                Note = leg.Note,
                CreatedAt = DateTime.UtcNow
            };
            entry.Lines.Add(line);

            // تحديث رصيد الحساب وفق طبيعته (مدين/دائن):
            // حساب طبيعته مدين يزداد بالمدين وينقص بالدائن، والعكس للحسابات الدائنة.
            account.CurrentBalance += account.NormalBalance == NormalBalance.Debit
                ? (leg.DebitAmount - leg.CreditAmount)
                : (leg.CreditAmount - leg.DebitAmount);
        }

        // تجهيز القيد في السياق (دون SaveChanges) ليكون المتصل حراً في حفظه ذرياً
        _context.Set<JournalEntry>().Add(entry);
        _context.Set<JournalEntryLine>().AddRange(entry.Lines);

        return entry;
    }
}