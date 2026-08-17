using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents an account in the Chart of Accounts (COA).
/// Supports a multi-level tree structure through self-referencing (ParentAccount).
/// Each account belongs to one of five main types and has a normal balance (Debit/Credit).
/// 
/// لماذا استخدمنا هيكل شجري (Parent/Children) بدلاً من جدول منفصل للفئات؟
/// - المرونة: يمكن إضافة مستويات غير محدودة (حساب رئيسي → فرعي → فرعي فرعي...)
/// - البساطة: علاقة واحدة (ParentId) تحل محل جداول متعددة
/// - التوافق مع المعايير المحاسبية التي تستخدم ترميزاً هرمياً (مثل 1-الأصول، 11-أصول متداولة، 111-نقدية)
/// </summary>
public class Account : BaseEntity
{
    /// <summary>
    /// Unique account code (e.g., "1100" for Cash).
    /// Used for ordering and quick reference.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the account in Arabic (e.g., "النقدية").
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the account in English (e.g., "Cash").
    /// Useful for bilingual reports and international standards.
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// The main category this account belongs to.
    /// Determines the normal balance and where it appears in financial statements.
    /// </summary>
    public AccountType AccountType { get; set; }

    /// <summary>
    /// Normal balance of this account.
    /// Assets and Expenses are Debit; Liabilities, Equity, and Revenue are Credit.
    /// </summary>
    public NormalBalance NormalBalance { get; set; }

    /// <summary>
    /// Foreign key to the parent account (nullable for root-level accounts).
    /// Null means this is a top-level account in the tree.
    /// </summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>
    /// Navigation property to the parent account.
    /// Used by EF Core to build the self-referencing relationship.
    /// </summary>
    public Account? ParentAccount { get; set; }

    /// <summary>
    /// Collection of child accounts.
    /// Together with ParentAccount, this forms the tree structure.
    /// </summary>
    public ICollection<Account> ChildAccounts { get; set; } = new List<Account>();

    /// <summary>
    /// Optional description of what this account is used for.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this account is active and can be used in transactions.
    /// Inactive accounts are hidden from dropdowns but retained for historical data.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system-defined account that cannot be deleted or renamed.
    /// Protects core accounts required by the system (e.g., default Sales Revenue account).
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Current balance of this account (sum of all posted journal entry lines).
    /// Computed from journal entries, not stored directly — stored here as a denormalized
    /// cache for performance in queries and the dashboard.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Concurrency token (SQL Server rowversion) — detects lost updates to the account balance.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Helper property: Returns the indentation level in the tree.
    /// Root accounts have Level 0, their children Level 1, etc.
    /// Used for UI display (indentation in tree views).
    /// This is NOT mapped to the database; it's computed at runtime.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Helper property: Full path of account codes from root to this account.
    /// Example: "1 > 11 > 111" for Cash under Current Assets.
    /// NOT mapped to database; computed at runtime for display purposes.
    /// </summary>
    public string? HierarchyPath { get; set; }

    /// <summary>
    /// Journal entry lines (legs) that reference this account.
    /// Allows traversing from an account to all its recorded movements.
    /// </summary>
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}

