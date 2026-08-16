using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements expense category business logic (فئات المصاريف).
/// Each category is linked to an expense account in the Chart of Accounts.
/// </summary>
public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly DbContext _context;

    public ExpenseCategoryService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<ExpenseCategoryDto>> GetAllAsync()
    {
        var categories = await _context.Set<ExpenseCategory>()
            .Include(c => c.Account)
            .Include(c => c.ExpenseEntries)
            .OrderBy(c => c.Code)
            .ToListAsync();

        return categories.Select(MapToDto).ToList();
    }

    public async Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryDto dto)
    {
        var codeExists = await _context.Set<ExpenseCategory>()
            .AnyAsync(c => c.Code == dto.Code && !c.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود فئة المصروف '{dto.Code}' موجود مسبقاً");

        var account = await GetAndValidateExpenseAccountAsync(dto.AccountId);

        var category = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            AccountId = account.Id,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ExpenseCategory>().Add(category);
        await _context.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<ExpenseCategoryDto> UpdateAsync(UpdateExpenseCategoryDto dto)
    {
        var category = await _context.Set<ExpenseCategory>()
            .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted);
        if (category is null)
            throw new InvalidOperationException("فئة المصروف غير موجودة");

        if (category.IsSystem)
            throw new InvalidOperationException("لا يمكن تعديل الفئات النظامية");

        var codeExists = await _context.Set<ExpenseCategory>()
            .AnyAsync(c => c.Code == dto.Code && c.Id != dto.Id && !c.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود فئة المصروف '{dto.Code}' موجود مسبقاً");

        var account = await GetAndValidateExpenseAccountAsync(dto.AccountId);

        category.Code = dto.Code;
        category.NameAr = dto.NameAr;
        category.NameEn = dto.NameEn;
        category.AccountId = account.Id;
        category.Description = dto.Description;
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _context.Set<ExpenseCategory>()
            .Include(c => c.ExpenseEntries)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (category is null)
            throw new InvalidOperationException("فئة المصروف غير موجودة");

        if (category.IsSystem)
            throw new InvalidOperationException("لا يمكن حذف الفئات النظامية");

        if (category.ExpenseEntries.Any(e => !e.IsDeleted))
            throw new InvalidOperationException("لا يمكن حذف الفئة لأنها مرتبطة بسندات مصروف");

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task<Account> GetAndValidateExpenseAccountAsync(Guid accountId)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted);

        if (account is null)
            throw new InvalidOperationException("حساب المصروف غير موجود");

        if (account.AccountType != AccountType.Expense)
            throw new InvalidOperationException("يجب اختيار حساب من نوع مصروفات");

        return account;
    }

    private static ExpenseCategoryDto MapToDto(ExpenseCategory category)
    {
        return new ExpenseCategoryDto
        {
            Id = category.Id,
            Code = category.Code,
            NameAr = category.NameAr,
            NameEn = category.NameEn,
            AccountId = category.AccountId,
            AccountCode = category.Account?.Code ?? string.Empty,
            AccountName = category.Account?.NameAr ?? string.Empty,
            Description = category.Description,
            IsActive = category.IsActive,
            IsSystem = category.IsSystem,
            EntriesCount = category.ExpenseEntries.Count(e => !e.IsDeleted)
        };
    }
}
