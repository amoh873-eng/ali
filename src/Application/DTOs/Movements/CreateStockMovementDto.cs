using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Movements;

/// <summary>
/// DTO for recording a single stock movement.
/// The user always enters a positive quantity — the service applies the sign
/// automatically based on MovementType (In types = +, Out types = -).
/// </summary>
public class CreateStockMovementDto
{
    [Required(ErrorMessage = "الصنف مطلوب")]
    public Guid ItemId { get; set; }

    [Required(ErrorMessage = "المخزن مطلوب")]
    public Guid WarehouseId { get; set; }

    [Required(ErrorMessage = "نوع الحركة مطلوب")]
    [Range(1, 7, ErrorMessage = "نوع الحركة غير صالح")]
    public int MovementType { get; set; }

    [Range(0.01, 999999999999, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
    public decimal Quantity { get; set; }

    [Range(0, 999999999999, ErrorMessage = "سعر التكلفة غير صالح")]
    public decimal UnitCost { get; set; }

    [StringLength(50)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    /// <summary>
    /// Business date of the movement (defaults to today).
    /// </summary>
    public DateTime MovementDate { get; set; } = DateTime.Today;
}