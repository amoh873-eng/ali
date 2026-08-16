using ERPSystem.Application.DTOs.Sales;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for sales return operations (مردودات المبيعات).
/// The create method reverses the original stock and journal entries.
/// </summary>
public interface ISalesReturnService
{
    /// <summary>
    /// Returns all sales returns (newest first).
    /// </summary>
    Task<List<SalesReturnDto>> GetReturnsAsync();

    /// <summary>
    /// Gets a single return including its lines.
    /// </summary>
    Task<SalesReturnDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates and posts a sales return linked to an original invoice:
    /// returns goods to stock and reverses the sale + COGS journal entries. Atomic.
    /// </summary>
    Task<SalesReturnDto> CreateAsync(CreateSalesReturnDto dto);
}
