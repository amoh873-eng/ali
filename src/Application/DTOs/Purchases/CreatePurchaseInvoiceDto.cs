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

    /// <summary>
    /// أسلوب سداد فاتورة المشتريات: null أو 1 = آجل (يُدين المورد 2200 — الوضع التاريخي)،
    /// 2 = شيك (تُسجَّل فاتورة على المورد + قيد إصدار شيك يخفض الالتزام فوراً).
    /// </summary>
    public int? PaymentMethod { get; set; }

    /// <summary>رقم الشيك المطبوع — مطلوب عندما PaymentMethod = 2.</summary>
    [StringLength(100)]
    public string? CheckNumber { get; set; }

    /// <summary>اسم البنك — مطلوب عندما PaymentMethod = 2.</summary>
    [StringLength(150)]
    public string? CheckBankName { get; set; }

    /// <summary>الفرع (اختياري).</summary>
    [StringLength(150)]
    public string? CheckBranch { get; set; }

    /// <summary>تاريخ كتابة الشيك (افتراضياً تاريخ الفاتورة).</summary>
    public DateTime? CheckIssueDate { get; set; }

    /// <summary>تاريخ الاستحقاق — مطلوب عندما PaymentMethod = 2.</summary>
    public DateTime? CheckDueDate { get; set; }
}