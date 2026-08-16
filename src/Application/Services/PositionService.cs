using ERPSystem.Application.DTOs.Positions;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements job position business logic (المسميات الوظيفية).
/// </summary>
public class PositionService : IPositionService
{
    private readonly DbContext _context;

    public PositionService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<PositionDto>> GetAllAsync()
    {
        var positions = await _context.Set<Position>()
            .Include(p => p.Department)
            .OrderBy(p => p.Code)
            .ToListAsync();

        return positions.Select(MapToDto).ToList();
    }

    public async Task<PositionDto?> GetByIdAsync(Guid id)
    {
        var position = await _context.Set<Position>()
            .Include(p => p.Department)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        return position is null ? null : MapToDto(position);
    }

    public async Task<PositionDto> CreateAsync(CreatePositionDto dto)
    {
        var codeExists = await _context.Set<Position>()
            .AnyAsync(p => p.Code == dto.Code && !p.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود المسمى '{dto.Code}' موجود مسبقاً");

        var departmentExists = await _context.Set<Department>()
            .AnyAsync(d => d.Id == dto.DepartmentId && !d.IsDeleted);
        if (!departmentExists)
            throw new InvalidOperationException("القسم غير موجود");

        var position = new Position
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            DepartmentId = dto.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Position>().Add(position);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(position.Id) ?? MapToDto(position);
    }

    public async Task<PositionDto> UpdateAsync(UpdatePositionDto dto)
    {
        var position = await _context.Set<Position>()
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted);

        if (position is null)
            throw new InvalidOperationException("المسمى الوظيفي غير موجود");

        var codeExists = await _context.Set<Position>()
            .AnyAsync(p => p.Code == dto.Code && p.Id != dto.Id && !p.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود المسمى '{dto.Code}' موجود مسبقاً");

        var departmentExists = await _context.Set<Department>()
            .AnyAsync(d => d.Id == dto.DepartmentId && !d.IsDeleted);
        if (!departmentExists)
            throw new InvalidOperationException("القسم غير موجود");

        position.Code = dto.Code;
        position.NameAr = dto.NameAr;
        position.NameEn = dto.NameEn;
        position.DepartmentId = dto.DepartmentId;
        position.IsActive = dto.IsActive;
        position.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(position.Id) ?? MapToDto(position);
    }

    public async Task DeleteAsync(Guid id)
    {
        var position = await _context.Set<Position>()
            .Include(p => p.Employees)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (position is null)
            throw new InvalidOperationException("المسمى الوظيفي غير موجود");

        if (position.IsSystem)
            throw new InvalidOperationException("لا يمكن حذف المسميات النظامية");

        if (position.Employees.Count > 0)
            throw new InvalidOperationException(
                $"لا يمكن حذف المسمى لأنه يشغله {position.Employees.Count} موظف.");

        position.IsDeleted = true;
        position.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var position = await _context.Set<Position>()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (position is null)
            throw new InvalidOperationException("المسمى الوظيفي غير موجود");

        position.IsActive = !position.IsActive;
        position.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static PositionDto MapToDto(Position p)
    {
        return new PositionDto
        {
            Id = p.Id,
            Code = p.Code,
            NameAr = p.NameAr,
            NameEn = p.NameEn,
            DepartmentId = p.DepartmentId,
            DepartmentNameAr = p.Department?.NameAr,
            IsActive = p.IsActive,
            IsSystem = p.IsSystem
        };
    }
}
