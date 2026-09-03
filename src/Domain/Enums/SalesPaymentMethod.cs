namespace ERPSystem.Domain.Enums;

/// <summary>
/// Determines the instrument used to settle a sale (أداة السداد للفاتورة).
/// Distinct from the expense voucher's PaymentMethod (نقداً/بنك) and from
/// <see cref="SalesInvoiceType"/>:
/// <see cref="SalesInvoiceType"/> describes how the customer settles (immediately
/// vs on-account) while SalesPaymentMethod describes the actual instrument and —
/// crucially — decides which asset account the accounting entry debits:
/// - Cash (نقدي)      → Cash account (1100)
/// - Card (بطاقة)     → Card Receivables account (1205) — the bank owes the business until settlement
/// - OnAccount (آجل)  → Accounts Receivable (1200)
///
/// ⚠️ PCI-DSS: card sales only ever add NON-sensitive metadata (approval code,
/// last 4 digits, network name) — never a full card number, CVV, or expiry.
/// </summary>
public enum SalesPaymentMethod
{
    /// <summary>دفع نقدي فوري (Cash)</summary>
    Cash = 1,

    /// <summary>دفع ببطاقة بنكية عبر طرفية (Card) — يُحال للبنك للتسوية لاحقاً</summary>
    Card = 2,

    /// <summary>بيع آجل — مبلغ مستحق على العميل (On-Account)</summary>
    OnAccount = 3
}