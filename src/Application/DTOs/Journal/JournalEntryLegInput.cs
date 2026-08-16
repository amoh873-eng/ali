namespace ERPSystem.Application.DTOs.Journal;

/// <summary>
/// One debit/credit leg passed to the journal posting helper.
/// Each leg must have exactly one of DebitAmount or CreditAmount equal to zero
/// (a single leg cannot be both a debit and a credit).
/// </summary>
public class JournalEntryLegInput
{
    public Guid AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Note { get; set; }
}
