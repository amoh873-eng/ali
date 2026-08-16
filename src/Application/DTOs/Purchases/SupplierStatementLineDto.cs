namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>
/// سطر في كشف حساب مورد: فاتورة (تزيد المستحق للمورد = دائن) أو مردود (ينقص = مدين).
/// </summary>
public class SupplierStatementLineDto
{
    public DateTime Date { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string TypeNameAr { get; set; } = string.Empty;
    public decimal Debit { get; set; }   // مدين - ينقص المستحق للمورد (مردود)
    public decimal Credit { get; set; }  // دائن - يزيد المستحق للمورد (فاتورة)
    public decimal Balance { get; set; } // الرصيد الجاري بعد العملية
}