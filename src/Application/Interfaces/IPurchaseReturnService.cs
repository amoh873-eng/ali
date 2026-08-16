using ERPSystem.Application.DTOs.Purchases;

namespace ERPSystem.Application.Interfaces;

/// <summary>Contract for purchase return operations (مردودات المشتريات).</summary>
public interface IPurchaseReturnService
{
    Task<List<PurchaseReturnDto>> GetReturnsAsync();
    Task<PurchaseReturnDto?> GetByIdAsync(Guid id);

    /// <summary>ينشئ ويرحّل مردود المشتريات: حركة صادر + قيد عكسي + تحديث الرصيد (ذرياً).</summary>
    Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnDto dto);
}