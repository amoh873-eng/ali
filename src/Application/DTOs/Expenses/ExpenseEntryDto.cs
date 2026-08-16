namespace ERPSystem.Application.DTOs.Expenses;

/// <summary>
/// DTO for displaying an expense voucher together with its category name.
/// </summary>
public class ExpenseEntryDto
{
    public Guid Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public Guid ExpenseCategoryId { get; set; }
    public string ExpenseCategoryCode { get; set; } = string.Empty;
    public string ExpenseCategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>
    /// 1 = Cash, 2 = Bank.
    /// </summary>
    public int PaymentMethod { get; set; }
    public string? Description { get; set; }
    public string? AttachmentPath { get; set; }
    public int Status { get; set; }
    public Guid? JournalEntryId { get; set; }
}
