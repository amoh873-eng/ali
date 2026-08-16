using ERPSystem.Application.DTOs.Accounts;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements account management business logic.
/// This is the core service for the Chart of Accounts module.
/// 
/// شرح منطق العمل:
/// - عند إنشاء حساب، نتحقق من: عدم تكرار الكود، وجود الحساب الأب، صحة النوع
/// - عند الحذف، نمنع حذف الحسابات النظامية أو التي لها أبناء
/// - نحدد NormalBalance تلقائياً من نوع الحساب:
///   الأصول والمصروفات → مدين، الخصوم وحقوق الملكية والإيرادات → دائن
/// </summary>
public class AccountService : IAccountService
{
    private readonly DbContext _context;

    public AccountService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountDto>> GetAccountTreeAsync()
    {
        var accounts = await _context.Set<Account>()
            .Where(a => !a.IsDeleted)
            .OrderBy(a => a.Code)
            .ToListAsync();

        // لماذا نبني الشجرة في الكود وليس في قاعدة البيانات؟
        // EF Core لا يدعم استعلامات تكرارية (Recursive CTEs) مباشرة بكفاءة.
        // البديل: نجلب كل الحسابات دفعة واحدة (Query واحد)، ثم نبني الشجرة في الذاكرة.
        return BuildTree(accounts, null);
    }

    public async Task<List<AccountDto>> GetAllAccountsAsync()
    {
        var accounts = await _context.Set<Account>()
            .Where(a => !a.IsDeleted)
            .OrderBy(a => a.Code)
            .ToListAsync();

        return accounts.Select(MapToDto).ToList();
    }

    public async Task<AccountDto?> GetByIdAsync(Guid id)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        return account is null ? null : MapToDto(account);
    }

    public async Task<AccountDto> CreateAsync(CreateAccountDto dto)
    {
        // التحقق من عدم تكرار الكود
        var codeExists = await _context.Set<Account>()
            .AnyAsync(a => a.Code == dto.Code && !a.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود الحساب '{dto.Code}' موجود مسبقاً");

        // التحقق من وجود الحساب الأب (إذا تم تحديده)
        Account? parent = null;
        if (dto.ParentAccountId.HasValue)
        {
            parent = await _context.Set<Account>()
                .FirstOrDefaultAsync(a => a.Id == dto.ParentAccountId.Value && !a.IsDeleted);

            if (parent is null)
                throw new InvalidOperationException("الحساب الأب غير موجود");
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            AccountType = (AccountType)dto.AccountType,
            // تحديد طبيعة الحساب تلقائياً من نوعه - لا نتركه للمستخدم يحدده
            NormalBalance = DetermineNormalBalance((AccountType)dto.AccountType),
            ParentAccountId = dto.ParentAccountId,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Account>().Add(account);
        await _context.SaveChangesAsync();

        return MapToDto(account);
    }

    public async Task<AccountDto> UpdateAsync(UpdateAccountDto dto)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Id == dto.Id && !a.IsDeleted);

        if (account is null)
            throw new InvalidOperationException("الحساب غير موجود");

        if (account.IsSystem)
            throw new InvalidOperationException("لا يمكن تعديل الحسابات النظامية");

        var codeExists = await _context.Set<Account>()
            .AnyAsync(a => a.Code == dto.Code && a.Id != dto.Id && !a.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود الحساب '{dto.Code}' موجود مسبقاً");

        if (dto.ParentAccountId == dto.Id)
            throw new InvalidOperationException("لا يمكن أن يكون الحساب أباً لنفسه");

        account.Code = dto.Code;
        account.NameAr = dto.NameAr;
        account.NameEn = dto.NameEn;
        account.AccountType = (AccountType)dto.AccountType;
        account.NormalBalance = DetermineNormalBalance((AccountType)dto.AccountType);
        account.ParentAccountId = dto.ParentAccountId;
        account.Description = dto.Description;
        account.IsActive = dto.IsActive;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(account);
    }

    public async Task DeleteAsync(Guid id)
    {
        var account = await _context.Set<Account>()
            .Include(a => a.ChildAccounts)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (account is null)
            throw new InvalidOperationException("الحساب غير موجود");

        if (account.IsSystem)
            throw new InvalidOperationException("لا يمكن حذف الحسابات النظامية");

        var activeChildren = account.ChildAccounts.Where(c => !c.IsDeleted).ToList();
        if (activeChildren.Any())
            throw new InvalidOperationException(
                $"لا يمكن حذف الحساب لأنه يحتوي على {activeChildren.Count} حساب فرعي. احذف الحسابات الفرعية أولاً.");

        account.IsDeleted = true;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (account is null)
            throw new InvalidOperationException("الحساب غير موجود");

        if (account.IsSystem)
            throw new InvalidOperationException("لا يمكن تعطيل الحسابات النظامية");

        account.IsActive = !account.IsActive;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
    // ==================== Helper Methods ====================

    /// <summary>
    /// Determines the normal balance based on account type.
    /// هذا منطق محاسبي أساسي:
    /// - الأصول والمصروفات: طبيعتها مدينة (تزيد بالمدين)
    /// - الخصوم وحقوق الملكية والإيرادات: طبيعتها دائنة (تزيد بالدائن)
    /// </summary>
    private static NormalBalance DetermineNormalBalance(AccountType type)
    {
        return type switch
        {
            AccountType.Asset => NormalBalance.Debit,
            AccountType.Expense => NormalBalance.Debit,
            AccountType.Liability => NormalBalance.Credit,
            AccountType.Equity => NormalBalance.Credit,
            AccountType.Revenue => NormalBalance.Credit,
            _ => NormalBalance.Debit
        };
    }

    /// <summary>
    /// Builds a hierarchical tree from a flat list of accounts.
    /// خوارزمية بناء الشجرة:
    /// 1. نأخذ الحسابات التي ParentAccountId لها يساوي parentId المعطى
    /// 2. لكل حساب، ننشئ DTO ونستدعي الدالة تكرارياً لبناء الأبناء
    /// 3. النتيجة: شجرة كاملة متعددة المستويات
    /// </summary>
    private List<AccountDto> BuildTree(List<Account> allAccounts, Guid? parentId, int level = 0)
    {
        return allAccounts
            .Where(a => a.ParentAccountId == parentId)
            .Select(a =>
            {
                var dto = MapToDto(a);
                dto.Children = BuildTree(allAccounts, a.Id, level + 1);
                return dto;
            })
            .ToList();
    }

    /// <summary>
    /// Maps a domain Account entity to an AccountDto.
    /// استخدام الـ Mapping اليدوي بدلاً من AutoMapper للتعلم والتحكم الكامل.
    /// </summary>
    private static AccountDto MapToDto(Account account)
    {
        return new AccountDto
        {
            Id = account.Id,
            Code = account.Code,
            NameAr = account.NameAr,
            NameEn = account.NameEn,
            AccountType = (int)account.AccountType,
            AccountTypeNameAr = GetAccountTypeNameAr(account.AccountType),
            NormalBalance = (int)account.NormalBalance,
            NormalBalanceNameAr = account.NormalBalance == NormalBalance.Debit ? "مدين" : "دائن",
            ParentAccountId = account.ParentAccountId,
            ParentAccountName = account.ParentAccount?.NameAr,
            Description = account.Description,
            IsActive = account.IsActive,
            IsSystem = account.IsSystem,
            CurrentBalance = account.CurrentBalance
        };
    }

    /// <summary>
    /// Returns the Arabic display name for an account type.
    /// </summary>
    private static string GetAccountTypeNameAr(AccountType type)
    {
        return type switch
        {
            AccountType.Asset => "أصول",
            AccountType.Liability => "خصوم",
            AccountType.Equity => "حقوق ملكية",
            AccountType.Revenue => "إيرادات",
            AccountType.Expense => "مصروفات",
            _ => "غير معروف"
        };
    }
}

