using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents an expense voucher (سند مصروف). When posted, it automatically creates
/// a balanced journal entry: the category's expense account is debited and the
/// cash/bank account is credited, using the same automatic posting style as sales invoices.
/// </summary>
public class ExpenseEntry : BaseEntity
{
    /// <summary>
    /// Unique human-readable voucher number (e.g., "EXP-20260815-0001").
    /// </summary>
    public string EntryNumber { get; set; } = string.Empty;

    /// <summary>
    /// Business date of the expense.
    /// </summary>
    public DateTime ExpenseDate { get; set; }

    /// <summary>
    /// Foreign key to the expense category.
    /// </summary>
    public Guid ExpenseCategoryId { get; set; }

    /// <summary>
    /// Navigation property to the expense category.
    /// </summary>
    public ExpenseCategory? ExpenseCategory { get; set; }

    /// <summary>
    /// The expense amount (always positive).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// How the expense was paid (cash or bank).
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Optional free-text description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional attachment path (relative URL under wwwroot, e.g., "uploads/....pdf").
    /// </summary>
    public string? AttachmentPath { get; set; }

    /// <summary>
    /// Lifecycle status (always Posted in this module, matching the immediate-posting style).
    /// </summary>
    public DocumentStatus Status { get; set; }

    /// <summary>
    /// The journal entry generated when this voucher was posted.
    /// </summary>
    public Guid? JournalEntryId { get; set; }
}
