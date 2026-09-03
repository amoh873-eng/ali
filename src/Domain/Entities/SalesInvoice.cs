using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a sales invoice (فاتورة بيع) issued to a customer.
///
/// نموذج الخصم والضريبة:
/// - SubTotal      = مجموع (كمية × سعر الوحدة) لجميع البنود
/// - DiscountAmount= SubTotal × DiscountPercentage / 100  (خصم فوري على رأس الفاتورة)
/// - TaxAmount     = (SubTotal − DiscountAmount) × TaxRate / 100  (ضريبة مبيعات مثل ضريبة الأردن)
/// - TotalAmount   = SubTotal − DiscountAmount + TaxAmount
/// هذا النموذج شائع وعملي لأنه يطبّق الخصم والضريبة على مستوى الفاتورة كاملة.
/// </summary>
public class SalesInvoice : BaseEntity
{
    /// <summary>
    /// Unique human-readable invoice number (e.g., "SI-20260201-0001").
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the customer.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Warehouse from which stock is issued.
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Navigation property to the warehouse.
    /// </summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// Business date of the invoice.
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Cash or On-Account (نقدي أو آجل) — how the customer settles the invoice.
    /// Used by AR aging and the customer balance.
    /// </summary>
    public SalesInvoiceType InvoiceType { get; set; }

    // ── Card payment (بطاقة) — nullable so cash/on-account records are unaffected ──
    // ⚠️ PCI-DSS: never store full card number, CVV, or expiry — only the reference
    // code, last 4 digits (display), and the card network name.

    /// <summary>
    /// How the sale was settled: نقدي / بطاقة / آجل. Defaults to Cash for legacy rows.
    /// Decides the debit account in the journal entry (1100 / 1205 / 1200).
    /// </summary>
    public SalesPaymentMethod PaymentMethod { get; set; } = SalesPaymentMethod.Cash;

    /// <summary>
    /// Approval / reference code the cashier copies from the card terminal's printed
    /// receipt. REQUIRED for card sales (non-empty) — this is what reconciliation
    /// matches against the bank statement.
    /// </summary>
    public string? CardApprovalCode { get; set; }

    /// <summary>
    /// Last 4 digits of the card (display only, helps matching). Optional.
    /// </summary>
    public string? CardLast4 { get; set; }

    /// <summary>
    /// Card network name (Visa / Mastercard / ...). Optional.
    /// </summary>
    public string? CardNetwork { get; set; }

    /// <summary>
    /// Timestamp of the card transaction on the terminal (if printed/shown). Optional.
    /// </summary>
    public DateTime? CardTransactionAt { get; set; }

    /// <summary>
    /// Lifecycle status (Draft / Posted / Cancelled).
    /// </summary>
    public DocumentStatus Status { get; set; }

    /// <summary>
    /// Sum of all line totals (before discount and tax).
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Header-level discount percentage (e.g., 5 = 5%).
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Computed discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Tax rate percentage applied on the discounted amount (e.g., 16 = 16%).
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Computed tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Final total = SubTotal − DiscountAmount + TaxAmount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Amount actually paid so far (equals TotalAmount for cash invoices).
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Due date for AR aging — nullable for backward compat, defaults to InvoiceDate if not set.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Optional free-text note.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Whether this invoice was issued from the Point of Sale (POS) screen.
    /// </summary>
    public bool IsPos { get; set; }

    /// <summary>
    /// Line items of this invoice.
    /// </summary>
    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();

    /// <summary>
    /// Returns linked to this invoice (reverse some or all of it).
    /// </summary>
    public ICollection<SalesReturn> SalesReturns { get; set; } = new List<SalesReturn>();

    /// <summary>
    /// The journal entry generated when this invoice was posted (to record the sale).
    /// Null while the invoice is still a draft.
    /// </summary>
    public Guid? SalesJournalEntryId { get; set; }

    /// <summary>
    /// The journal entry generated for the cost of goods sold (COGS).
    /// Null while the invoice is still a draft.
    /// </summary>
    public Guid? CogsJournalEntryId { get; set; }

    // ── Part F: JoFotara e-invoicing ──
    public string? JoFotaraQrCode { get; set; }
    public DateTime? JoFotaraSubmittedAt { get; set; }
    public string? JoFotaraReferenceNumber { get; set; }
    public JoFotaraStatus JoFotaraStatus { get; set; } = JoFotaraStatus.NotSubmitted;
}
