namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for a customer statement (كشف حساب عميل): header info + ordered lines.
/// </summary>
public class CustomerStatementDto
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<CustomerStatementLineDto> Lines { get; set; } = new();
}
