using ERPSystem.Application.DTOs.Employees;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements employee business logic (الموظفون).
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly DbContext _context;

    public EmployeeService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<EmployeeDto>> GetAllAsync()
    {
        var employees = await _context.Set<Employee>()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .OrderBy(e => e.EmployeeNumber)
            .ToListAsync();

        return employees.Select(MapToDto).ToList();
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id)
    {
        var employee = await _context.Set<Employee>()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        return employee is null ? null : MapToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        await ValidateAsync(dto.EmployeeNumber, dto.DepartmentId, dto.PositionId, Guid.Empty);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = dto.EmployeeNumber,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            DepartmentId = dto.DepartmentId,
            PositionId = dto.PositionId,
            HireDate = dto.HireDate,
            Status = dto.Status,
            BasicSalary = dto.BasicSalary,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Employee>().Add(employee);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(employee.Id) ?? MapToDto(employee);
    }

    public async Task<EmployeeDto> UpdateAsync(UpdateEmployeeDto dto)
    {
        var employee = await _context.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Id == dto.Id && !e.IsDeleted);

        if (employee is null)
            throw new InvalidOperationException("الموظف غير موجود");

        await ValidateAsync(dto.EmployeeNumber, dto.DepartmentId, dto.PositionId, dto.Id);

        employee.EmployeeNumber = dto.EmployeeNumber;
        employee.NameAr = dto.NameAr;
        employee.NameEn = dto.NameEn;
        employee.DepartmentId = dto.DepartmentId;
        employee.PositionId = dto.PositionId;
        employee.HireDate = dto.HireDate;
        employee.Status = dto.Status;
        employee.BasicSalary = dto.BasicSalary;
        employee.Phone = dto.Phone;
        employee.Email = dto.Email;
        employee.Address = dto.Address;
        employee.Notes = dto.Notes;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(employee.Id) ?? MapToDto(employee);
    }

    public async Task DeleteAsync(Guid id)
    {
        var employee = await _context.Set<Employee>()
            .Include(e => e.Leaves)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (employee is null)
            throw new InvalidOperationException("الموظف غير موجود");

        if (employee.Leaves.Count > 0)
            throw new InvalidOperationException(
                $"لا يمكن حذف الموظف لوجود {employee.Leaves.Count} طلب إجازة مرتبط به");

        employee.IsDeleted = true;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<EmployeeDto> SetStatusAsync(Guid id, EmployeeStatus status)
    {
        var employee = await _context.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (employee is null)
            throw new InvalidOperationException("الموظف غير موجود");

        employee.Status = status;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(employee.Id) ?? MapToDto(employee);
    }

    private async Task ValidateAsync(string employeeNumber, Guid departmentId, Guid positionId, Guid excludeId)
    {
        var numberExists = await _context.Set<Employee>()
            .AnyAsync(e => e.EmployeeNumber == employeeNumber && e.Id != excludeId && !e.IsDeleted);
        if (numberExists)
            throw new InvalidOperationException($"الرقم الوظيفي '{employeeNumber}' موجود مسبقاً");

        var departmentExists = await _context.Set<Department>()
            .AnyAsync(d => d.Id == departmentId && !d.IsDeleted);
        if (!departmentExists)
            throw new InvalidOperationException("القسم غير موجود");

        var positionExists = await _context.Set<Position>()
            .AnyAsync(p => p.Id == positionId && !p.IsDeleted);
        if (!positionExists)
            throw new InvalidOperationException("المسمى الوظيفي غير موجود");
    }

    private static EmployeeDto MapToDto(Employee e)
    {
        return new EmployeeDto
        {
            Id = e.Id,
            EmployeeNumber = e.EmployeeNumber,
            NameAr = e.NameAr,
            NameEn = e.NameEn,
            DepartmentId = e.DepartmentId,
            DepartmentNameAr = e.Department?.NameAr,
            PositionId = e.PositionId,
            PositionNameAr = e.Position?.NameAr,
            HireDate = e.HireDate,
            Status = e.Status,
            BasicSalary = e.BasicSalary,
            Phone = e.Phone,
            Email = e.Email,
            Address = e.Address,
            Notes = e.Notes
        };
    }
}
