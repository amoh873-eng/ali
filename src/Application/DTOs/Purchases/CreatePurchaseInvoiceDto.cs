using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Purchases;

/// <summary>DTO لإنشاء (وترحيل) فاتورة مشتريات.</summary>
public class CreatePurchaseInvoiceDto
{
    [Required(ErrorMessage = "المورد مطلوب")]
    public Guid SupplierId { get; set; }

    [Required(ErrorMessage = "المخزن مطلوب")]
    public Guid WarehouseId { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    public DateTime? DueDate { get; set; }

    [Range(1, 2, ErrorMessage = "نوع الفاتورة غير صالح")]
    public int InvoiceType { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    [MinLength(1, ErrorMessage = "يجب إضافة بند واحد على الأقل")]
    public List<CreatePurchaseInvoiceLineDto> Lines { get; set; } = new();
}