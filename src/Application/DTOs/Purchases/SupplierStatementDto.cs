namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>DTO لكشف حساب مورد (رأس + بنود مرتبة).</summary>
public class SupplierStatementDto
{
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<SupplierStatementLineDto> Lines { get; set; } = new();
}