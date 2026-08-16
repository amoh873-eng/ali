using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements expense voucher business logic (سندات المصروف) with full accounting integration.
///
/// عند ترحيل سند مصروف ننفّذ ذرياً (في نفس SaveChanges):
/// قيد محاسبي متوازن — مدين: حساب المصروف (من الفئة) / دائن: الصندوق (نقداً) أو البنك.
/// </summary>
public class ExpenseService : IExpenseService
{
    // أكواد الحسابات النظامية (يجب تطابقها مع بذور شجرة الحسابات)
    private const string AccountCash = "1100";
    private const string AccountBank = "1101";

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public ExpenseService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    public async Task<List<ExpenseEntryDto>> GetEntriesAsync(Guid? categoryId = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Set<ExpenseEntry>()
            .Include(e => e.ExpenseCategory)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);

        if (from.HasValue)
            query = query.Where(e => e.ExpenseDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(e => e.ExpenseDate <= to.Value.Date);

        var entries = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();

        return entries.Select(MapToDto).ToList();
    }

    public async Task<ExpenseEntryDto?> GetByIdAsync(Guid id)
    {
        var entry = await _context.Set<ExpenseEntry>()
            .Include(e => e.ExpenseCategory)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        return entry is null ? null : MapToDto(entry);
    }

    public async Task<ExpenseEntryDto> CreateAsync(CreateExpenseEntryDto dto)
    {
        var category = await _context.Set<ExpenseCategory>()
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.Id == dto.ExpenseCategoryId && !c.IsDeleted);

        if (category is null)
            throw new InvalidOperationException("فئة المصروف غير موجودة");

        if (category.Account is null)
            throw new InvalidOperationException("الفئة غير مرتبطة بحساب مصروف");

        if (dto.Amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

        var method = (PaymentMethod)dto.PaymentMethod;
        if (method != PaymentMethod.Cash && method != PaymentMethod.Bank)
            throw new InvalidOperationException("طريقة الدفع غير صالحة");

        var paymentAccount = await GetAccountByCodeAsync(method == PaymentMethod.Cash ? AccountCash : AccountBank);

        var entry = new ExpenseEntry
        {
            Id = Guid.NewGuid(),
            EntryNumber = await NumberSequenceHelper.NextAsync(_context, "EXP"),
            ExpenseDate = dto.ExpenseDate,
            ExpenseCategoryId = category.Id,
            Amount = Math.Round(dto.Amount, 2),
            PaymentMethod = method,
            Description = dto.Description,
            AttachmentPath = dto.AttachmentPath,
            Status = DocumentStatus.Posted,
            CreatedAt = DateTime.UtcNow
        };

        // القيد المحاسبي: مدين حساب المصروف / دائن الصندوق أو البنك
        var journal = await _journalService.PrepareEntryAsync(
            JournalEntryType.Expense,
            entry.ExpenseDate,
            entry.EntryNumber,
            $"سند مصروف {entry.EntryNumber} - {category.NameAr}",
            new List<JournalEntryLegInput>
            {
                new() { AccountId = category.Account.Id, DebitAmount = entry.Amount },
                new() { AccountId = paymentAccount.Id, CreditAmount = entry.Amount }
            });

        entry.JournalEntryId = journal.Id;

        _context.Set<ExpenseEntry>().Add(entry);
        await _context.SaveChangesAsync();

        return MapToDto(entry);
    }

    private async Task<Account> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);

        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي بالكود '{code}' غير موجود");

        return account;
    }

    private static ExpenseEntryDto MapToDto(ExpenseEntry entry)
    {
        return new ExpenseEntryDto
        {
            Id = entry.Id,
            EntryNumber = entry.EntryNumber,
            ExpenseDate = entry.ExpenseDate,
            ExpenseCategoryId = entry.ExpenseCategoryId,
            ExpenseCategoryCode = entry.ExpenseCategory?.Code ?? string.Empty,
            ExpenseCategoryName = entry.ExpenseCategory?.NameAr ?? string.Empty,
            Amount = entry.Amount,
            PaymentMethod = (int)entry.PaymentMethod,
            Description = entry.Description,
            AttachmentPath = entry.AttachmentPath,
            Status = (int)entry.Status,
            JournalEntryId = entry.JournalEntryId
        };
    }
}
