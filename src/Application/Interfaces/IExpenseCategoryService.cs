using ERPSystem.Application.DTOs.Expenses;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for expense category operations (فئات المصاريف).
/// </summary>
public interface IExpenseCategoryService
{
    /// <summary>
    /// Returns all expense categories with their linked expense accounts.
    /// </summary>
    Task<List<ExpenseCategoryDto>> GetAllAsync();

    /// <summary>
    /// Creates a new expense category. Validates code uniqueness and that the
    /// linked account exists and is of type Expense.
    /// </summary>
    Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryDto dto);

    /// <summary>
    /// Updates an existing expense category.
    /// </summary>
    Task<ExpenseCategoryDto> UpdateAsync(UpdateExpenseCategoryDto dto);

    /// <summary>
    /// Soft-deletes an expense category. Fails if it has expense vouchers.
    /// </summary>
    Task DeleteAsync(Guid id);
}
