namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// نتيجة بحث سريع عن فاتورة مرحّلة (لمردودات نقطة البيع) — رأس فقط بلا بنود
/// (تُجلب البنود عبر GetByIdAsync عند اختيار فاتورة).
/// </summary>
public class SalesInvoiceSearchResultDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int PaymentMethod { get; set; }
    public string PaymentMethodNameAr => PaymentMethod switch { 2 => "بطاقة", 3 => "آجل", _ => "نقدي" };
    public bool IsPos { get; set; }
}
