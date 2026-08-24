using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// عقد خدمة الحضور اليومي — upsert + استعلامات للرواتب.
/// </summary>
public interface IAttendanceService
{
    Task MarkAttendanceAsync(Guid employeeId, DateOnly date, AttendanceStatus status, TimeSpan? checkIn = null, TimeSpan? checkOut = null, string? notes = null);
    Task<List<Domain.Entities.AttendanceRecord>> GetAttendanceForPeriodAsync(Guid? employeeId, DateOnly from, DateOnly to);
    Task<int> GetAbsenceCountAsync(Guid employeeId, DateOnly from, DateOnly to);
    Task<int> GetUnpaidDeductionDaysAsync(Guid employeeId, DateOnly from, DateOnly to);
}
