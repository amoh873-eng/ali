using ERPSystem.Application.DTOs.Expenses;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for expense voucher operations (سندات المصروف).
/// </summary>
public interface IExpenseService
{
    /// <summary>
    /// Returns expense vouchers (newest first), optionally filtered by category and date range.
    /// </summary>
    Task<List<ExpenseEntryDto>> GetEntriesAsync(Guid? categoryId = null, DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Gets a single expense voucher.
    /// </summary>
    Task<ExpenseEntryDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates and posts a new expense voucher: validates, then generates the
    /// automatic balanced journal entry (expense account debited, cash/bank credited).
    /// Atomic (single SaveChanges).
    /// </summary>
    Task<ExpenseEntryDto> CreateAsync(CreateExpenseEntryDto dto);
}
