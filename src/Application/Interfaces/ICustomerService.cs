using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.DTOs.Sales;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for customer operations (العملاء).
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Returns all active customers ordered by code.
    /// </summary>
    Task<List<CustomerDto>> GetAllAsync();

    /// <summary>
    /// Returns customers matching a search term (code or name).
    /// </summary>
    Task<List<CustomerDto>> SearchAsync(string search);

    /// <summary>
    /// Gets a single customer by id.
    /// </summary>
    Task<CustomerDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new customer. Validates code uniqueness.
    /// </summary>
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    Task<CustomerDto> UpdateAsync(UpdateCustomerDto dto);

    /// <summary>
    /// Soft-deletes a customer. Fails if it has posted documents.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggles the active status of a customer.
    /// </summary>
    Task ToggleActiveAsync(Guid id);

    /// <summary>
    /// Returns a customer statement (كشف حساب عميل) between two dates,
    /// listing invoices and returns with a running balance.
    /// </summary>
    Task<CustomerStatementDto> GetStatementAsync(Guid customerId, DateTime from, DateTime to);
}
