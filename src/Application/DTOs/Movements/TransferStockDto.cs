using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Movements;

/// <summary>
/// DTO for transferring stock between two warehouses.
/// The service creates two atomic movements:
/// TransferOut from the source warehouse and TransferIn to the destination.
/// </summary>
public class TransferStockDto
{
    [Required(ErrorMessage = "الصنف مطلوب")]
    public Guid ItemId { get; set; }

    [Required(ErrorMessage = "مخزن المصدر مطلوب")]
    public Guid SourceWarehouseId { get; set; }

    [Required(ErrorMessage = "مخزن الوجهة مطلوب")]
    public Guid DestinationWarehouseId { get; set; }

    [Range(0.01, 999999999999, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
    public decimal Quantity { get; set; }

    [StringLength(50)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public DateTime MovementDate { get; set; } = DateTime.Today;
}