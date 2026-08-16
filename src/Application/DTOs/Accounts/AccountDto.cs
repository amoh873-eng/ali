namespace ERPSystem.Application.DTOs.Accounts;

/// <summary>
/// Data Transfer Object for displaying account information in lists and tree views.
/// Separates the domain entity from what the UI sees (Separation of Concerns).
/// </summary>
public class AccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public int AccountType { get; set; }
    public string AccountTypeNameAr { get; set; } = string.Empty;
    public int NormalBalance { get; set; }
    public string NormalBalanceNameAr { get; set; } = string.Empty;
    public Guid? ParentAccountId { get; set; }
    public string? ParentAccountName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Child accounts for building the tree in the UI.
    /// </summary>
    public List<AccountDto> Children { get; set; } = new();
}
