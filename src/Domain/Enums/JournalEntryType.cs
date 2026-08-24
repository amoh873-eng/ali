namespace ERPSystem.Domain.Enums;

/// <summary>
/// Identifies the source that created a journal entry (قيد محاسبي).
/// يُستخدم للأرشفة والتتبّع: يعرف المستخدم من أين جاء القيد
/// (فاتورة مبيعات؟ مردود؟ قيد يدوي؟) عبر النوع والرقم المرجعي.
/// </summary>
public enum JournalEntryType
{
    /// <summary>قيد يدوي - Manual (ينشئه المستخدم مباشرة)</summary>
    Manual = 1,

    /// <summary>ناتج عن فاتورة مبيعات - Sales Invoice</summary>
    SalesInvoice = 2,

    /// <summary>ناتج عن مردود مبيعات - Sales Return</summary>
    SalesReturn = 3,

    /// <summary>ناتج عن فاتورة مشتريات - Purchase Invoice</summary>
    PurchaseInvoice = 4,

    /// <summary>ناتج عن مردود مشتريات - Purchase Return</summary>
    PurchaseReturn = 5,

    /// <summary>ناتج عن تسوية مخزون - Stock Adjustment</summary>
    StockAdjustment = 6,

    /// <summary>ناتج عن سند مصروف - Expense</summary>
    Expense = 7,

    /// <summary>ناتج عن دورة رواتب - Payroll</summary>
    Payroll = 8
}
