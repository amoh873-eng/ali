using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements warehouse business logic (المخازن).
/// Rules:
/// - الكود فريد
/// - مخزن واحد فقط يمكن أن يكون الافتراضي
/// - لا يمكن حذف مخزن نظامي أو مخزن له حركات
/// </summary>
public class WarehouseService : IWarehouseService
{
    private readonly DbContext _context;

    public WarehouseService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<WarehouseDto>> GetAllAsync()
    {
        var warehouses = await _context.Set<Warehouse>()
            .OrderBy(w => w.Code)
            .ToListAsync();

        return warehouses.Select(MapToDto).ToList();
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id)
    {
        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

        return warehouse is null ? null : MapToDto(warehouse);
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto)
    {
        var codeExists = await _context.Set<Warehouse>()
            .AnyAsync(w => w.Code == dto.Code && !w.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود المخزن '{dto.Code}' موجود مسبقاً");

        // أول مخزن يتم إنشاؤه يصبح افتراضياً تلقائياً
        var hasAnyWarehouse = await _context.Set<Warehouse>().AnyAsync(w => !w.IsDeleted);

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            Location = dto.Location,
            IsActive = true,
            IsDefault = dto.IsDefault || !hasAnyWarehouse,
            CreatedAt = DateTime.UtcNow
        };

        // إذا كان هذا المخزن افتراضياً، نلغي الافتراضية عن البقية
        if (warehouse.IsDefault)
            await ClearDefaultAsync();

        _context.Set<Warehouse>().Add(warehouse);
        await _context.SaveChangesAsync();

        return MapToDto(warehouse);
    }

    public async Task<WarehouseDto> UpdateAsync(UpdateWarehouseDto dto)
    {
        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == dto.Id && !w.IsDeleted);

        if (warehouse is null)
            throw new InvalidOperationException("المخزن غير موجود");

        var codeExists = await _context.Set<Warehouse>()
            .AnyAsync(w => w.Code == dto.Code && w.Id != dto.Id && !w.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود المخزن '{dto.Code}' موجود مسبقاً");

        warehouse.Code = dto.Code;
        warehouse.NameAr = dto.NameAr;
        warehouse.NameEn = dto.NameEn;
        warehouse.Location = dto.Location;
        warehouse.IsActive = dto.IsActive;

        if (dto.IsDefault && !warehouse.IsDefault)
        {
            await ClearDefaultAsync();
            warehouse.IsDefault = true;
        }

        warehouse.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(warehouse);
    }

    public async Task DeleteAsync(Guid id)
    {
        var warehouse = await _context.Set<Warehouse>()
            .Include(w => w.StockMovements)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

        if (warehouse is null)
            throw new InvalidOperationException("المخزن غير موجود");

        if (warehouse.IsSystem)
            throw new InvalidOperationException("لا يمكن حذف المخازن النظامية");

        if (warehouse.StockMovements.Count > 0)
            throw new InvalidOperationException(
                "لا يمكن حذف المخزن لأنه يحتوي على حركات مخزون مسجلة.");

        warehouse.IsDeleted = true;
        warehouse.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

        if (warehouse is null)
            throw new InvalidOperationException("المخزن غير موجود");

        warehouse.IsActive = !warehouse.IsActive;
        warehouse.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task SetDefaultAsync(Guid id)
    {
        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

        if (warehouse is null)
            throw new InvalidOperationException("المخزن غير موجود");

        await ClearDefaultAsync();
        warehouse.IsDefault = true;
        warehouse.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// Clears the IsDefault flag from all warehouses.
    /// </summary>
    private async Task ClearDefaultAsync()
    {
        var defaults = await _context.Set<Warehouse>()
            .Where(w => w.IsDefault && !w.IsDeleted)
            .ToListAsync();

        foreach (var w in defaults)
            w.IsDefault = false;
    }

    private static WarehouseDto MapToDto(Warehouse warehouse)
    {
        return new WarehouseDto
        {
            Id = warehouse.Id,
            Code = warehouse.Code,
            NameAr = warehouse.NameAr,
            NameEn = warehouse.NameEn,
            Location = warehouse.Location,
            IsActive = warehouse.IsActive,
            IsSystem = warehouse.IsSystem,
            IsDefault = warehouse.IsDefault
        };
    }
}
