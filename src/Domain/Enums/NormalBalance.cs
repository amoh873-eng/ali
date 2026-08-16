namespace ERPSystem.Domain.Enums;

/// <summary>
/// Determines which side (Debit or Credit) normally increases the account balance.
/// This is a fundamental accounting concept:
/// - Assets and Expenses: normal balance is Debit (increase with debit)
/// - Liabilities, Equity, and Revenue: normal balance is Credit (increase with credit)
/// </summary>
public enum NormalBalance
{
    /// <summary>مدين - Debit side</summary>
    Debit = 1,

    /// <summary>دائن - Credit side</summary>
    Credit = 2
}
