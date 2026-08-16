namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// سطر في دفتر أستاذ حساب: حركة مدين/دائن مع رصيد جارٍ.
/// </summary>
public class LedgerLineDto
{
    public DateTime Date { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}