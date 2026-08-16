namespace ERPSystem.Application.DTOs.Reports;

/// <summary>
/// دفتر أستاذ حساب (كشف حساب دفتر أستاذ): الرصيد الافتتاحي والحركات والرصيد الختامي.
/// </summary>
public class AccountLedgerDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountNameAr { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<LedgerLineDto> Lines { get; set; } = new();
}