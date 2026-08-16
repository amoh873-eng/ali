using ERPSystem.Application.DTOs.Accounts;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Defines the contract for account-related business operations.
/// The Web layer depends on this interface, not on the concrete implementation
/// (Dependency Inversion Principle - the 'D' in SOLID).
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Returns all active accounts as a hierarchical tree structure.
    /// </summary>
    Task<List<AccountDto>> GetAccountTreeAsync();

    /// <summary>
    /// Returns a flat list of all accounts (useful for dropdowns and search).
    /// </summary>
    Task<List<AccountDto>> GetAllAccountsAsync();

    /// <summary>
    /// Gets a single account by its unique identifier.
    /// </summary>
    Task<AccountDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new account. Validates uniqueness of code and parent existence.
    /// Returns the created account DTO.
    /// </summary>
    Task<AccountDto> CreateAsync(CreateAccountDto dto);

    /// <summary>
    /// Updates an existing account. Returns the updated account DTO.
    /// </summary>
    Task<AccountDto> UpdateAsync(UpdateAccountDto dto);

    /// <summary>
    /// Soft-deletes an account (sets IsDeleted = true).
    /// Fails if the account has child accounts or journal entries.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggles the active status of an account.
    /// </summary>
    Task ToggleActiveAsync(Guid id);
}
