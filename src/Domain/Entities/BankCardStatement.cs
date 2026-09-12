using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// One row imported from the bank's card-settlement statement (كشف تسوية البنك للبطاقات).
/// The cashier/banker downloads this file periodically from the bank portal and imports it
/// in the card reconciliation screen to confirm that every card sale recorded in the system
/// actually reached the bank.
///
/// ⚠️ PCI-DSS: this table stores NO card number, CVV, or expiry — only the approval/reference
/// code taken from the printed terminal receipt plus amount and date, which is all that is
/// needed to match against the bank statement.
/// </summary>
public class BankCardStatement : BaseEntity
{
    /// <summary>Approval / reference number of the card transaction (رقم الموافقة من إيصال الطرفية).</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Transaction amount in JOD (المبلغ).</summary>
    public decimal Amount { get; set; }

    /// <summary>Transaction date from the bank statement (التاريخ).</summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>Timestamp of when this row was imported into the system.</summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Display name / username of the user who performed the import (اختياري).</summary>
    public string? ImportedBy { get; set; }
}