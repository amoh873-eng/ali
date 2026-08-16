using ERPSystem.Application.DTOs.Departments;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements organizational department business logic (الأقسام).
/// Departments support a tree structure through self-referencing, like categories.
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly DbContext _context;

    public DepartmentService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> GetTreeAsync()
    {
        var departments = await _context.Set<Department>()
            .OrderBy(d => d.Code)
            .ToListAsync();

        return BuildTree(departments, null);
    }

    public async Task<List<DepartmentDto>> GetAllAsync()
    {
        var departments = await _context.Set<Department>()
            .Include(d => d.ParentDepartment)
            .OrderBy(d => d.Code)
            .ToListAsync();

        return departments.Select(MapToDto).ToList();
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        var department = await _context.Set<Department>()
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        return department is null ? null : MapToDto(department);
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
    {
        var codeExists = await _context.Set<Department>()
            .AnyAsync(d => d.Code == dto.Code && !d.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود القسم '{dto.Code}' موجود مسبقاً");

        if (dto.ParentDepartmentId.HasValue)
        {
            var parentExists = await _context.Set<Department>()
                .AnyAsync(d => d.Id == dto.ParentDepartmentId.Value && !d.IsDeleted);
            if (!parentExists)
                throw new InvalidOperationException("القسم الأب غير موجود");
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            ParentDepartmentId = dto.ParentDepartmentId,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Department>().Add(department);
        await _context.SaveChangesAsync();

        return MapToDto(department);
    }

    public async Task<DepartmentDto> UpdateAsync(UpdateDepartmentDto dto)
    {
        var department = await _context.Set<Department>()
            .FirstOrDefaultAsync(d => d.Id == dto.Id && !d.IsDeleted);

        if (department is null)
            throw new InvalidOperationException("القسم غير موجود");

        var codeExists = await _context.Set<Department>()
            .AnyAsync(d => d.Code == dto.Code && d.Id != dto.Id && !d.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود القسم '{dto.Code}' موجود مسبقاً");

        // منع جعل القسم ابنًا لنفسه (يخلق حلقة لا نهائية في الشجرة)
        if (dto.ParentDepartmentId.HasValue && dto.ParentDepartmentId.Value == dto.Id)
            throw new InvalidOperationException("لا يمكن جعل القسم ابنًا لنفسه");

        department.Code = dto.Code;
        department.NameAr = dto.NameAr;
        department.NameEn = dto.NameEn;
        department.ParentDepartmentId = dto.ParentDepartmentId;
        department.Description = dto.Description;
        department.IsActive = dto.IsActive;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(department);
    }

    public async Task DeleteAsync(Guid id)
    {
        var department = await _context.Set<Department>()
            .Include(d => d.ChildDepartments)
            .Include(d => d.Employees)
            .Include(d => d.Positions)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (department is null)
            throw new InvalidOperationException("القسم غير موجود");

        if (department.IsSystem)
            throw new InvalidOperationException("لا يمكن حذف الأقسام النظامية");

        if (department.ChildDepartments.Count > 0)
            throw new InvalidOperationException("لا يمكن حذف القسم لوجود أقسام فرعية تابعة له");

        if (department.Employees.Count > 0)
            throw new InvalidOperationException("لا يمكن حذف القسم لوجود موظفين مرتبطين به");

        if (department.Positions.Count > 0)
            throw new InvalidOperationException("لا يمكن حذف القسم لوجود مسميات وظيفية مرتبطة به");

        department.IsDeleted = true;
        department.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var department = await _context.Set<Department>()
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (department is null)
            throw new InvalidOperationException("القسم غير موجود");

        department.IsActive = !department.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ==================== Helper Methods ====================

    private static List<DepartmentDto> BuildTree(List<Department> all, Guid? parentId)
    {
        return all
            .Where(d => d.ParentDepartmentId == parentId)
            .Select(d =>
            {
                var dto = MapToDto(d);
                dto.Children = BuildTree(all, d.Id);
                return dto;
            })
            .ToList();
    }

    private static DepartmentDto MapToDto(Department d)
    {
        return new DepartmentDto
        {
            Id = d.Id,
            Code = d.Code,
            NameAr = d.NameAr,
            NameEn = d.NameEn,
            ParentDepartmentId = d.ParentDepartmentId,
            ParentDepartmentName = d.ParentDepartment?.NameAr,
            Description = d.Description,
            IsActive = d.IsActive,
            IsSystem = d.IsSystem
        };
    }
}
