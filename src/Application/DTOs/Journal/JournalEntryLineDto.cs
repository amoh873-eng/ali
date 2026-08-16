namespace ERPSystem.Application.DTOs.Journal;

/// <summary>
/// DTO for a journal entry line (بند قيد) with account info for display.
/// </summary>
public class JournalEntryLineDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountNameAr { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Note { get; set; }
}
