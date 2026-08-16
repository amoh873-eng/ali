using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Expenses;

/// <summary>
/// DTO for creating (and immediately posting) an expense voucher.
/// </summary>
public class CreateExpenseEntryDto
{
    [Required]
    public Guid ExpenseCategoryId { get; set; }

    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// 1 = Cash, 2 = Bank.
    /// </summary>
    [Required]
    [Range(1, 2)]
    public int PaymentMethod { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? AttachmentPath { get; set; }
}
