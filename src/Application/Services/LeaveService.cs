using ERPSystem.Application.DTOs.Leaves;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements leave request business logic (الإجازات).
/// </summary>
public class LeaveService : ILeaveService
{
    private readonly DbContext _context;

    public LeaveService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveDto>> GetAllAsync()
    {
        var leaves = await _context.Set<Leave>()
            .Include(l => l.Employee)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();

        return leaves.Select(MapToDto).ToList();
    }

    public async Task<LeaveDto?> GetByIdAsync(Guid id)
    {
        var leave = await _context.Set<Leave>()
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        return leave is null ? null : MapToDto(leave);
    }

    public async Task<LeaveDto> CreateAsync(CreateLeaveDto dto)
    {
        var employeeExists = await _context.Set<Employee>()
            .AnyAsync(e => e.Id == dto.EmployeeId && !e.IsDeleted);
        if (!employeeExists)
            throw new InvalidOperationException("الموظف غير موجود");

        if (dto.EndDate < dto.StartDate)
            throw new InvalidOperationException("تاريخ نهاية الإجازة لا يسبق تاريخ بدايتها");

        // منع تداخل طلب الإجازة الجديد مع طلب آخر معلّق/معتمد لنفس الموظف في نفس الفترة
        var overlapping = await _context.Set<Leave>()
            .AnyAsync(l => l.EmployeeId == dto.EmployeeId
                           && !l.IsDeleted
                           && (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved)
                           && l.StartDate <= dto.EndDate
                           && l.EndDate >= dto.StartDate);
        if (overlapping)
            throw new InvalidOperationException("يوجد طلب إجازة آخر متداخل مع هذه الفترة لنفس الموظف.");

        var leave = new Leave
        {
            Id = Guid.NewGuid(),
            EmployeeId = dto.EmployeeId,
            LeaveType = dto.LeaveType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = LeaveStatus.Pending,
            Reason = dto.Reason,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Leave>().Add(leave);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(leave.Id) ?? MapToDto(leave);
    }

    public async Task<LeaveDto> SetStatusAsync(Guid id, LeaveStatus status)
    {
        var leave = await _context.Set<Leave>()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (leave is null)
            throw new InvalidOperationException("طلب الإجازة غير موجود");

        leave.Status = status;
        leave.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(leave.Id) ?? MapToDto(leave);
    }

    public async Task DeleteAsync(Guid id)
    {
        var leave = await _context.Set<Leave>()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (leave is null)
            throw new InvalidOperationException("طلب الإجازة غير موجود");

        leave.IsDeleted = true;
        leave.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static LeaveDto MapToDto(Leave l)
    {
        return new LeaveDto
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeNameAr = l.Employee?.NameAr,
            EmployeeNumber = l.Employee?.EmployeeNumber,
            LeaveType = l.LeaveType,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            Status = l.Status,
            Reason = l.Reason
        };
    }
}
