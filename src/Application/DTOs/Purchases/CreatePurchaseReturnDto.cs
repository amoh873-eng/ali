using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>DTO لإنشاء (وترحيل) مردود مشتريات مرتبط بفاتورة أصلية.</summary>
public class CreatePurchaseReturnDto
{
    [Required(ErrorMessage = "الفاتورة الأصلية مطلوبة")]
    public Guid PurchaseInvoiceId { get; set; }

    [Required(ErrorMessage = "المخزن مطلوب")]
    public Guid WarehouseId { get; set; }

    public DateTime ReturnDate { get; set; } = DateTime.Today;

    [StringLength(1000)]
    public string? Note { get; set; }

    [MinLength(1, ErrorMessage = "يجب إضافة بند واحد على الأقل")]
    public List<CreatePurchaseReturnLineDto> Lines { get; set; } = new();
}