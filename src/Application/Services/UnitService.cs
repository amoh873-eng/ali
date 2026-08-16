using ERPSystem.Application.DTOs.Units;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements unit-of-measure business logic (وحدات القياس).
/// Validation rules:
/// - الكود فريد (لا يمكن تكراره بين الوحدات)
/// - لا يمكن حذف وحدة نظامية أو وحدة مستخدمة في أصناف
/// </summary>
public class UnitService : IUnitService
{
    private readonly DbContext _context;

    public UnitService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<UnitDto>> GetAllAsync()
    {
        var units = await _context.Set<Unit>()
            .OrderBy(u => u.Code)
            .ToListAsync();

        return units.Select(MapToDto).ToList();
    }

    public async Task<UnitDto?> GetByIdAsync(Guid id)
    {
        var unit = await _context.Set<Unit>()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        return unit is null ? null : MapToDto(unit);
    }

    public async Task<UnitDto> CreateAsync(CreateUnitDto dto)
    {
        var codeExists = await _context.Set<Unit>()
            .AnyAsync(u => u.Code == dto.Code && !u.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود الوحدة '{dto.Code}' موجود مسبقاً");

        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Unit>().Add(unit);
        await _context.SaveChangesAsync();

        return MapToDto(unit);
    }

    public async Task<UnitDto> UpdateAsync(UpdateUnitDto dto)
    {
        var unit = await _context.Set<Unit>()
            .FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted);

        if (unit is null)
            throw new InvalidOperationException("الوحدة غير موجودة");

        var codeExists = await _context.Set<Unit>()
            .AnyAsync(u => u.Code == dto.Code && u.Id != dto.Id && !u.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود الوحدة '{dto.Code}' موجود مسبقاً");

        unit.Code = dto.Code;
        unit.NameAr = dto.NameAr;
        unit.NameEn = dto.NameEn;
        unit.IsActive = dto.IsActive;
        unit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(unit);
    }

    public async Task DeleteAsync(Guid id)
    {
        var unit = await _context.Set<Unit>()
            .Include(u => u.Items)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (unit is null)
            throw new InvalidOperationException("الوحدة غير موجودة");

        if (unit.IsSystem)
            throw new InvalidOperationException("لا يمكن حذف الوحدات النظامية");

        // الوحدات المرتبطة بأصناف نشطة لا يمكن حذفها
        if (unit.Items.Count > 0)
            throw new InvalidOperationException(
                $"لا يمكن حذف الوحدة لأنها مستخدمة في {unit.Items.Count} صنف.");

        unit.IsDeleted = true;
        unit.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var unit = await _context.Set<Unit>()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (unit is null)
            throw new InvalidOperationException("الوحدة غير موجودة");

        unit.IsActive = !unit.IsActive;
        unit.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ==================== Helper Methods ====================

    private static UnitDto MapToDto(Unit unit)
    {
        return new UnitDto
        {
            Id = unit.Id,
            Code = unit.Code,
            NameAr = unit.NameAr,
            NameEn = unit.NameEn,
            IsActive = unit.IsActive,
            IsSystem = unit.IsSystem
        };
    }
}
