using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>Implements supplier business logic (الموردون).</summary>
public partial class SupplierService : ISupplierService
{
    private readonly DbContext _context;

    public SupplierService(DbContext context) => _context = context;

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _context.Set<Supplier>().OrderBy(s => s.Code).ToListAsync();
        return suppliers.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id)
    {
        var supplier = await _context.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        return supplier is null ? null : MapToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
    {
        var codeExists = await _context.Set<Supplier>().AnyAsync(s => s.Code == dto.Code && !s.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود المورد '{dto.Code}' موجود مسبقاً");

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Code = dto.Code, NameAr = dto.NameAr, NameEn = dto.NameEn,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address,
            TaxNumber = dto.TaxNumber, CreditLimit = dto.CreditLimit, Notes = dto.Notes,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };

        _context.Set<Supplier>().Add(supplier);
        await _context.SaveChangesAsync();
        return MapToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(UpdateSupplierDto dto)
    {
        var supplier = await _context.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == dto.Id && !s.IsDeleted);
        if (supplier is null) throw new InvalidOperationException("المورد غير موجود");

        var codeExists = await _context.Set<Supplier>().AnyAsync(s => s.Code == dto.Code && s.Id != dto.Id && !s.IsDeleted);
        if (codeExists) throw new InvalidOperationException($"كود المورد '{dto.Code}' موجود مسبقاً");

        supplier.Code = dto.Code;
        supplier.NameAr = dto.NameAr;
        supplier.NameEn = dto.NameEn;
        supplier.Phone = dto.Phone;
        supplier.Email = dto.Email;
        supplier.Address = dto.Address;
        supplier.TaxNumber = dto.TaxNumber;
        supplier.CreditLimit = dto.CreditLimit;
        supplier.Notes = dto.Notes;
        supplier.IsActive = dto.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(supplier);
    }

    public async Task DeleteAsync(Guid id)
    {
        var supplier = await _context.Set<Supplier>()
            .Include(s => s.PurchaseInvoices)
            .Include(s => s.PurchaseReturns)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (supplier is null) throw new InvalidOperationException("المورد غير موجود");
        if (supplier.PurchaseInvoices.Any() || supplier.PurchaseReturns.Any())
            throw new InvalidOperationException("لا يمكن حذف مورد له فواتير أو مردودات مسجلة.");

        supplier.IsDeleted = true;
        supplier.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var supplier = await _context.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (supplier is null) throw new InvalidOperationException("المورد غير موجود");

        supplier.IsActive = !supplier.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static SupplierDto MapToDto(Supplier s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        NameAr = s.NameAr,
        NameEn = s.NameEn,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address,
        TaxNumber = s.TaxNumber,
        CreditLimit = s.CreditLimit,
        CurrentBalance = s.CurrentBalance,
        IsActive = s.IsActive,
        IsSystem = s.IsSystem,
        Notes = s.Notes
    };
}