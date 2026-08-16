namespace ERPSystem.Domain.Enums;

/// <summary>
/// Represents the main categories of accounts in the Chart of Accounts.
/// Based on standard accounting principles (5 main types).
/// </summary>
public enum AccountType
{
    /// <summary>الأصول - Assets (what the company owns)</summary>
    Asset = 1,

    /// <summary>الخصوم - Liabilities (what the company owes)</summary>
    Liability = 2,

    /// <summary>حقوق الملكية - Equity (owner's claim on assets)</summary>
    Equity = 3,

    /// <summary>الإيرادات - Revenue (income from operations)</summary>
    Revenue = 4,

    /// <summary>المصروفات - Expenses (costs of operations)</summary>
    Expense = 5
}
