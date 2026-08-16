using ERPSystem.Application.DTOs.Purchases;

namespace ERPSystem.Application.Interfaces;

/// <summary>Contract for purchase invoice operations (فواتير المشتريات).</summary>
public interface IPurchaseInvoiceService
{
    Task<List<PurchaseInvoiceDto>> GetInvoicesAsync();
    Task<PurchaseInvoiceDto?> GetByIdAsync(Guid id);

    /// <summary>ينشئ ويرحّل فاتورة المشتريات: حركة وارد + قيد محاسبي + تحديث الرصيد (ذرياً).</summary>
    Task<PurchaseInvoiceDto> CreateAsync(CreatePurchaseInvoiceDto dto);
}