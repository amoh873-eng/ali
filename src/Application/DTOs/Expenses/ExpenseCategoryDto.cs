namespace ERPSystem.Application.DTOs.Expenses;

/// <summary>
/// DTO for displaying an expense category with its linked expense account.
/// </summary>
public class ExpenseCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }

    /// <summary>
    /// Number of expense vouchers linked to this category.
    /// </summary>
    public int EntriesCount { get; set; }
}
