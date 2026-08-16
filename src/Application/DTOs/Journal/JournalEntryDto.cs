namespace ERPSystem.Application.DTOs.Journal;

/// <summary>
/// DTO for displaying a journal entry (قيد محاسبي) header with its lines.
/// </summary>
public class JournalEntryDto
{
    public Guid Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public int EntryType { get; set; }
    public string EntryTypeNameAr { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced => TotalDebit == TotalCredit;
    public List<JournalEntryLineDto> Lines { get; set; } = new();
}
