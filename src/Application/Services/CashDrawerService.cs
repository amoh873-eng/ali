using ERPSystem.Application.DTOs.CashDrawer;
using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// تحويلات النقد بين الخزنة الرئيسية «الصندوق» (1100) ودرج الكاش (1105).
///
/// ليست مصروفاً ولا إيراداً — نقل نفس المال بين حسابين نقديين. كل تحويل يسجَّل
/// كعملية CashDrawerTransaction + قيد محاسبي متوازن في معاملة واحدة ذرية:
///   FloatIn (الخزنة ← درج الكاش): مدين «درج الكاش» 1105 / دائن «الصندوق» 1100
///   CashOut (درج الكاش ← الخزنة): مدين «الصندوق» 1100 / دائن «درج الكاش» 1105
/// نفس أسلوب الترحيل الذري المُتّبع في بقية الخدمات (SalesInvoice/StockWriteOff).
/// </summary>
public class CashDrawerService : ICashDrawerService
{
    // نفس الحسابين النظاميين المزروعين في SeedSalesAccounts (يجب تطابقهما تماماً)
    private const string AccountMainSafe = "1100";   // الخزنة الرئيسية «الصندوق»
    private const string AccountTillDrawer = "1105"; // درج الكاش

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public CashDrawerService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    public async Task<CashDrawerTransactionDto> CreateAsync(CreateCashTransferDto dto, string cashierUserId)
    {
        if (dto.Amount <= 0m)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر.");

        if (!Enum.IsDefined(typeof(CashTransferDirection), dto.Type))
            throw new InvalidOperationException("اتجاه التحويل غير صالح.");

        var safeAccount = await GetAccountByCodeAsync(AccountMainSafe);
        var tillAccount = await GetAccountByCodeAsync(AccountTillDrawer);

        var transfer = new CashDrawerTransaction
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            Amount = dto.Amount,
            Timestamp = dto.Timestamp,
            CashierUserId = cashierUserId,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var reference = await NumberSequenceHelper.NextAsync(_context, "CT");

        // وصف القيد حسب الاتجاه
        var description = dto.Type == CashTransferDirection.FloatIn
            ? $"تمويل درج الكاش من الخزنة {reference}"
            : $"سحب نقد من درج الكاش إلى الخزنة {reference}";

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // القيد المتوازن حسب الاتجاه:
            // FloatIn : مدين درج الكاش / دائن الصندوق
            // CashOut : مدين الصندوق / دائن درج الكاش
            var entry = dto.Type == CashTransferDirection.FloatIn
                ? await _journalService.PrepareEntryAsync(
                    JournalEntryType.CashTransfer,
                    dto.Timestamp,
                    reference,
                    description,
                    new List<JournalEntryLegInput>
                    {
                        new() { AccountId = tillAccount.Id, DebitAmount = dto.Amount },
                        new() { AccountId = safeAccount.Id, CreditAmount = dto.Amount }
                    })
                : await _journalService.PrepareEntryAsync(
                    JournalEntryType.CashTransfer,
                    dto.Timestamp,
                    reference,
                    description,
                    new List<JournalEntryLegInput>
                    {
                        new() { AccountId = safeAccount.Id, DebitAmount = dto.Amount },
                        new() { AccountId = tillAccount.Id, CreditAmount = dto.Amount }
                    });

            transfer.JournalEntryId = entry.Id;

            _context.Set<CashDrawerTransaction>().Add(transfer);

            // SaveChanges واحدة: العملية + القيد + أرصدة الحسابين ذرياً
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return MapToDto(transfer, entry);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<CashDrawerTransactionDto>> GetAsync(CashDrawerFilterDto filter)
    {
        var query = _context.Set<CashDrawerTransaction>()
            .Include(t => t.JournalEntry)
            .AsQueryable();

        if (filter.From.HasValue)
            query = query.Where(t => t.Timestamp >= filter.From.Value);
        if (filter.To.HasValue)
        {
            var to = filter.To.Value.Date.AddDays(1);
            query = query.Where(t => t.Timestamp < to);
        }
        if (filter.Type.HasValue)
            query = query.Where(t => t.Type == filter.Type.Value);

        var rows = await query
            .OrderByDescending(t => t.Timestamp)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        // فلترة الكاشير في الذاكرة (لا يوجد جدول لربط المعرف بالاسم داخل الخدمة)
        if (!string.IsNullOrWhiteSpace(filter.Cashier))
        {
            var term = filter.Cashier.Trim();
            rows = rows.Where(t =>
                t.CashierUserId.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return rows.Select(t => MapToDto(t)).ToList();
    }

    public async Task<CashDrawerTransactionDto?> GetByIdAsync(Guid id)
    {
        var transfer = await _context.Set<CashDrawerTransaction>()
            .Include(t => t.JournalEntry)
            .FirstOrDefaultAsync(t => t.Id == id);

        return transfer is null ? null : MapToDto(transfer, transfer.JournalEntry);
    }

    private async Task<Account> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي '{code}' غير موجود في شجرة الحسابات.");
        return account;
    }

    private static CashDrawerTransactionDto MapToDto(CashDrawerTransaction t, JournalEntry? entry = null)
        => new()
        {
            Id = t.Id,
            Type = t.Type,
            Amount = t.Amount,
            Timestamp = t.Timestamp,
            CashierUserId = t.CashierUserId,
            Notes = t.Notes,
            JournalEntryId = t.JournalEntryId,
            EntryNumber = entry?.EntryNumber ?? t.JournalEntry?.EntryNumber ?? "—",
            EntryDescription = entry?.Description ?? t.JournalEntry?.Description ?? string.Empty
        };
}

