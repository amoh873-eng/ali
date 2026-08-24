namespace ERPSystem.Application.DTOs.Reports;

public class BillingStageReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<BillingStageInvoiceDto> Invoices { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public int SubmittedCount { get; set; }
    public int FailedCount { get; set; }
    public int NotSubmittedCount { get; set; }
}
public class BillingStageInvoiceDto
{
    public string InvoiceNumber { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int JoFotaraStatus { get; set; }
}
