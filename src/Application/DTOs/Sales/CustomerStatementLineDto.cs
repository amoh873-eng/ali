namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// A single row in a customer statement (كشف حساب عميل).
/// Each row represents an invoice (increases the balance) or a return (decreases it),
/// followed by a running balance.
/// </summary>
public class CustomerStatementLineDto
{
    public DateTime Date { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string TypeNameAr { get; set; } = string.Empty;
    public decimal Debit { get; set; }   // مدين - يزيد على العميل (فاتورة)
    public decimal Credit { get; set; }  // دائن - ينقص من العميل (مردود/دفعة)
    public decimal Balance { get; set; } // الرصيد الجاري بعد هذه العملية
}
