using ERPSystem.Application.DTOs.Purchases;

namespace ERPSystem.Application.Interfaces;

/// <summary>Contract for supplier operations (الموردون).</summary>
public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllAsync();

    /// <summary>بحث في قاعدة البيانات بـ ILike على الكود والاسم (غير حساس للحالة — PostgreSQL).</summary>
    Task<List<SupplierDto>> SearchAsync(string term);

    Task<SupplierDto?> GetByIdAsync(Guid id);
    Task<SupplierDto> CreateAsync(CreateSupplierDto dto);
    Task<SupplierDto> UpdateAsync(UpdateSupplierDto dto);
    Task DeleteAsync(Guid id);
    Task ToggleActiveAsync(Guid id);

    /// <summary>كشف حساب مورد بين تاريخين (فواتير ومردودات برصيد جارٍ).</summary>
    Task<SupplierStatementDto> GetStatementAsync(Guid supplierId, DateTime from, DateTime to);
}