using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents an expense category (فئة المصروف) linked to an expense account
/// in the Chart of Accounts. Every expense voucher is classified under one category,
/// and the category determines which expense account is debited in the journal entry.
/// </summary>
public class ExpenseCategory : BaseEntity
{
    /// <summary>
    /// Unique category code (e.g., "RENT", "UTIL").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Category name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Category name in English.
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Foreign key to the expense account in the Chart of Accounts.
    /// This account is debited when an expense voucher of this category is posted.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Navigation property to the linked expense account.
    /// </summary>
    public Account? Account { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this category is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system category that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Expense vouchers classified under this category.
    /// </summary>
    public ICollection<ExpenseEntry> ExpenseEntries { get; set; } = new List<ExpenseEntry>();
}
