using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// يمثل المورد (Supplier) الذي نشتري منه البضاعة.
/// كيان مستقل (مثل العميل) يُدار ببياناته، ويرتبط محاسبياً عبر حساب "الموردين/الدائنون".
/// </summary>
public class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }

    /// <summary>حد ائتماني اختياري (null = غير محدود).</summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>رصيد المورد الجاري (موجب = مستحق لنا عليه).</summary>
    public decimal CurrentBalance { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = false;
    public string? Notes { get; set; }

    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();
}