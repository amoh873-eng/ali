using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.DTOs.RepCustody;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// عهدة نقدية بيد المندوبين — تطبيق حساب واحد عام (1110) + بُعد تحليلي بـ RepUserId.
/// كل عملية (تحصيل ميداني أو تسليم للمكتب) يسجِّل سطر RepCashCustody + قيداً محاسبياً
/// متوازناً في معاملة واحدة ذرية (نفس أسلوب CashDrawerService/SalesInvoiceService):
///   Collect : مدين «عهدة نقدية بيد المندوبين» 1110 / دائن «ذمم العملاء» 1200
///   Delivery: مدين «الصندوق» 1100 / دائن «عهدة نقدية بيد المندوبين» 1110
/// </summary>
public class RepCustodyService : IRepCustodyService
{
    // الحسابات النظامية — يجب تطابقها مع بذر الهجرة ومثيلات SeedSalesAccounts تماماً
    private const string AccountCustody = "1110";   // عهدة نقدية بيد المندوبين (أصل/مدين)
    private const string AccountReceivable = "1200"; // ذمم العملاء
    private const string AccountMainSafe = "1100";   // الخزنة الرئيسية «الصندوق»

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public RepCustodyService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    public async Task<RepCustodyDto> CreateAsync(CreateRepCustodyDto dto, string actorUserId)
    {
        if (dto.Amount <= 0m)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر.");

        if (dto.Effect != RepCustodyEffect.Collect && dto.Effect != RepCustodyEffect.Delivery)
            throw new InvalidOperationException("نوع العملية غير صالح.");

        if (string.IsNullOrWhiteSpace(dto.RepUserId))
            throw new InvalidOperationException("يجب تحديد المندوب.");

        var custodyAccount = await GetAccountByCodeAsync(AccountCustody);
        var receivableAccount = dto.Effect == RepCustodyEffect.Collect
            ? await GetAccountByCodeAsync(AccountReceivable)
            : null;
        var safeAccount = dto.Effect == RepCustodyEffect.Delivery
            ? await GetAccountByCodeAsync(AccountMainSafe)
            : null;

        var custody = new RepCashCustody
        {
            Id = Guid.NewGuid(),
            Effect = dto.Effect,
            RepUserId = dto.RepUserId,
            CustomerId = dto.CustomerId,
            InvoiceId = dto.InvoiceId,
            PaymentMethod = dto.PaymentMethod,
            Amount = dto.Amount,
            Timestamp = dto.Timestamp,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var reference = await NumberSequenceHelper.NextAsync(_context, "RC");
        var description = dto.Effect == RepCustodyEffect.Collect
            ? $"تحصيل ميداني من عميل ضمن عهدة المندوب {reference}"
            : $"تسليم عهدة المندوب للمكتب {reference}";

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // القيد المتوازن حسب الأثر:
            // Collect : مدين عهدة المندوبين / دائن ذمم العملاء
            // Delivery: مدين الصندوق 1100 / دائن عهدة المندوبين
            var entry = dto.Effect == RepCustodyEffect.Collect
                ? await _journalService.PrepareEntryAsync(
                    JournalEntryType.CashCustody,
                    dto.Timestamp,
                    reference,
                    description,
                    new List<JournalEntryLegInput>
                    {
                        new() { AccountId = custodyAccount.Id, DebitAmount = dto.Amount },
                        new() { AccountId = receivableAccount!.Id, CreditAmount = dto.Amount }
                    })
                : await _journalService.PrepareEntryAsync(
                    JournalEntryType.CashCustody,
                    dto.Timestamp,
                    reference,
                    description,
                    new List<JournalEntryLegInput>
                    {
                        new() { AccountId = safeAccount!.Id, DebitAmount = dto.Amount },
                        new() { AccountId = custodyAccount.Id, CreditAmount = dto.Amount }
                    });

            custody.JournalEntryId = entry.Id;
            _context.Set<RepCashCustody>().Add(custody);

            // الحفظ على مرحلتين داخل نفس المعاملة: القيد أولاً (الفلاتر العامة على JournalEntry
            // تعطّل ترتيب الحفظ التلقائي المتعلق بالمفاتيح الأجنبية)، ثم العملية.
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return MapToDto(custody, entry);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

public async Task<List<RepCustodyBalanceDto>> GetBalancesAsync()
    {
        var rows = await _context.Set<RepCashCustody>()
            .OrderBy(t => t.Timestamp)
            .ToListAsync();

        // تجميع حسب المندوب (البُعد التحليلي المعتمد)
        var balances = new List<RepCustodyBalanceDto>();
        foreach (var r in rows)
        {
            var existing = balances.FirstOrDefault(b => b.RepUserId == r.RepUserId);
            if (existing is null)
            {
                balances.Add(new RepCustodyBalanceDto
                {
                    RepUserId = r.RepUserId,
                    Balance = r.Effect == RepCustodyEffect.Collect ? r.Amount : -r.Amount
                });
            }
            else
            {
                existing.Balance += r.Effect == RepCustodyEffect.Collect ? r.Amount : -r.Amount;
            }
        }
        return balances;
    }

    public async Task<List<RepCustodyDto>> GetAsync(RepCustodyFilterDto filter)
    {
        var query = _context.Set<RepCashCustody>()
            .Include(t => t.JournalEntry)
            .AsQueryable();

        if (filter.From.HasValue)
            query = query.Where(t => t.Timestamp >= filter.From.Value);
        if (filter.To.HasValue)
        {
            var to = filter.To.Value.Date.AddDays(1);
            query = query.Where(t => t.Timestamp < to);
        }

        var rows = await query
            .OrderByDescending(t => t.Timestamp)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(filter.Rep))
        {
            var term = filter.Rep.Trim();
            rows = rows.Where(t => t.RepUserId.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return rows.Select(t => MapToDto(t)).ToList();
    }

    private async Task<Account> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي '{code}' غير موجود في شجرة الحسابات.");
        return account;
    }

    private static RepCustodyDto MapToDto(RepCashCustody t, JournalEntry? entry = null)
        => new()
        {
            Id = t.Id,
            Effect = t.Effect,
            RepUserId = t.RepUserId,
            CustomerId = t.CustomerId,
            InvoiceId = t.InvoiceId,
            PaymentMethod = t.PaymentMethod,
            Amount = t.Amount,
            Timestamp = t.Timestamp,
            Notes = t.Notes,
            JournalEntryId = t.JournalEntryId,
            EntryNumber = entry?.EntryNumber ?? t.JournalEntry?.EntryNumber ?? "—",
            EntryDescription = entry?.Description ?? t.JournalEntry?.Description ?? string.Empty
        };
}