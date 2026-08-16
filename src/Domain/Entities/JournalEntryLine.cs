using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a single debit or credit leg of a journal entry (بند مدين/دائن في القيد).
/// لكل بند: إما DebitAmount موجبة وإما CreditAmount موجبة (نادراً الاثنان معاً).
/// القيد متوازن إذا كان Σ(debits) == Σ(credits).
/// </summary>
public class JournalEntryLine : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent journal entry.
    /// </summary>
    public Guid JournalEntryId { get; set; }

    /// <summary>
    /// Navigation property to the parent journal entry.
    /// </summary>
    public JournalEntry? JournalEntry { get; set; }

    /// <summary>
    /// Foreign key to the account this leg affects.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Navigation property to the account.
    /// </summary>
    public Account? Account { get; set; }

    /// <summary>
    /// Debit amount (> 0 when this leg is a debit).
    /// </summary>
    public decimal DebitAmount { get; set; }

    /// <summary>
    /// Credit amount (> 0 when this leg is a credit).
    /// </summary>
    public decimal CreditAmount { get; set; }

    /// <summary>
    /// Optional note describing this specific leg.
    /// </summary>
    public string? Note { get; set; }
}
