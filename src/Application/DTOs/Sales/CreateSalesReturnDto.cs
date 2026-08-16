using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Sales;

/// <summary>
/// DTO for creating (and immediately posting) a sales return linked to an original invoice.
/// </summary>
public class CreateSalesReturnDto
{
    [Required(ErrorMessage = "الفاتورة الأصلية مطلوبة")]
    public Guid SalesInvoiceId { get; set; }

    [Required(ErrorMessage = "المخزن مطلوب")]
    public Guid WarehouseId { get; set; }

    public DateTime ReturnDate { get; set; } = DateTime.Today;

    [StringLength(1000)]
    public string? Note { get; set; }

    [MinLength(1, ErrorMessage = "يجب إضافة بند واحد على الأقل")]
    public List<CreateSalesReturnLineDto> Lines { get; set; } = new();
}
