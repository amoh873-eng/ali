using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// تنفيذ منطق الحضور اليومي — upsert + مزامنة مع الإجازات عند حساب الخصم.
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly DbContext _context;
    public AttendanceService(DbContext context) => _context = context;

    public async Task MarkAttendanceAsync(Guid employeeId, DateOnly date, AttendanceStatus status, TimeSpan? checkIn = null, TimeSpan? checkOut = null, string? notes = null)
    {
        var exists = await _context.Set<Employee>().AnyAsync(e => e.Id == employeeId && !e.IsDeleted);
        if (!exists) throw new InvalidOperationException("الموظف غير موجود");
        var rec = await _context.Set<AttendanceRecord>()
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == date && !a.IsDeleted);
        if (rec is null)
        {
            _context.Set<AttendanceRecord>().Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(), EmployeeId = employeeId, Date = date, Status = status,
                CheckInTime = checkIn, CheckOutTime = checkOut, Notes = notes, CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            rec.Status = status; rec.CheckInTime = checkIn; rec.CheckOutTime = checkOut;
            rec.Notes = notes; rec.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<AttendanceRecord>> GetAttendanceForPeriodAsync(Guid? employeeId, DateOnly from, DateOnly to)
    {
        var q = _context.Set<AttendanceRecord>().Include(a => a.Employee)
            .Where(a => !a.IsDeleted && a.Date >= from && a.Date <= to);
        if (employeeId.HasValue) q = q.Where(a => a.EmployeeId == employeeId.Value);
        var records = await q.OrderBy(a => a.Date).ToListAsync();

        // مزامنة تلقائية مع الإجازات المعتمدة: أي يوم ضمن إجازة معتمدة ولم يُسجّل حضوره يُعاد كـ OnLeave
        // حتى لا يُطلب من HR إدخال مزدوج بين وحدة الإجازات والحضور.
        var fromDt = from.ToDateTime(TimeOnly.MinValue).Date;
        var toDt = to.ToDateTime(TimeOnly.MinValue).Date;
        var leaveQuery = _context.Set<Leave>().Where(l => !l.IsDeleted && l.Status == LeaveStatus.Approved && l.StartDate.Date <= toDt && l.EndDate.Date >= fromDt);
        if (employeeId.HasValue) leaveQuery = leaveQuery.Where(l => l.EmployeeId == employeeId.Value);
        var leaves = await leaveQuery.ToListAsync();
        if (leaves.Count == 0) return records;

        // نبني خريطة للأيام المغطاة بإجازة
        var leaveDays = new Dictionary<Guid, HashSet<DateOnly>>();
        foreach (var lv in leaves)
        {
            var s = lv.StartDate.Date < fromDt ? fromDt : lv.StartDate.Date;
            var e = lv.EndDate.Date > toDt ? toDt : lv.EndDate.Date;
            for (var d = s; d <= e; d = d.AddDays(1))
            {
                var dd = DateOnly.FromDateTime(d);
                if (!leaveDays.TryGetValue(lv.EmployeeId, out var set)) { set = new HashSet<DateOnly>(); leaveDays[lv.EmployeeId] = set; }
                set.Add(dd);
            }
        }

        var existingKeys = new HashSet<(Guid, DateOnly)>(records.Select(r => (r.EmployeeId, r.Date)));
        foreach (var kv in leaveDays)
        {
            foreach (var d in kv.Value)
            {
                if (existingKeys.Contains((kv.Key, d))) continue;
                // سجل افتراضي OnLeave للعرض فقط (لا يُحفظ تلقائياً — يُحفظ عند تأكيد الحفظ من الواجهة)
                records.Add(new AttendanceRecord { EmployeeId = kv.Key, Date = d, Status = AttendanceStatus.OnLeave, CreatedAt = DateTime.UtcNow });
            }
        }
        return records.OrderBy(a => a.Date).ThenBy(a => a.EmployeeId).ToList();
    }

    public async Task<int> GetAbsenceCountAsync(Guid employeeId, DateOnly from, DateOnly to)
        => await _context.Set<AttendanceRecord>().CountAsync(a => a.EmployeeId == employeeId && !a.IsDeleted && a.Date >= from && a.Date <= to && a.Status == AttendanceStatus.Absent);

    /// <summary>
    /// يحسب أيام الخصم: Absent + أيام إجازة Unpaid المعتمدة غير المسجلة كحضور، مع تجنب العد المزدوج.
    /// </summary>
    public async Task<int> GetUnpaidDeductionDaysAsync(Guid employeeId, DateOnly from, DateOnly to)
    {
        var absentDates = await _context.Set<AttendanceRecord>()
            .Where(a => a.EmployeeId == employeeId && !a.IsDeleted && a.Date >= from && a.Date <= to && a.Status == AttendanceStatus.Absent)
            .Select(a => a.Date).ToListAsync();

        // استخدام DateOnly المحوّل في الاستعلام مباشرة لتجنب مشكلة تحويل DateTime.Date غير المدعومة
        var fromDt = from.ToDateTime(TimeOnly.MinValue).Date;
        var toDt = to.ToDateTime(TimeOnly.MinValue).Date;
        var unpaidLeaves = await _context.Set<Leave>()
            .Where(l => l.EmployeeId == employeeId && !l.IsDeleted && l.Status == LeaveStatus.Approved && l.LeaveType == LeaveType.Unpaid
                        && l.StartDate.Date <= toDt && l.EndDate.Date >= fromDt).ToListAsync();

        var unpaidDays = new HashSet<DateOnly>();
        foreach (var lv in unpaidLeaves)
        {
            var s = lv.StartDate.Date < fromDt ? fromDt : lv.StartDate.Date;
            var e = lv.EndDate.Date > toDt ? toDt : lv.EndDate.Date;
            for (var d = s; d <= e; d = d.AddDays(1)) unpaidDays.Add(DateOnly.FromDateTime(d));
        }
        var presentDates = await _context.Set<AttendanceRecord>()
            .Where(a => a.EmployeeId == employeeId && !a.IsDeleted && a.Date >= from && a.Date <= to && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late))
            .Select(a => a.Date).ToListAsync();
        var presentSet = new HashSet<DateOnly>(presentDates);
        var absentSet = new HashSet<DateOnly>(absentDates);
        int extra = unpaidDays.Count(d => !absentSet.Contains(d) && !presentSet.Contains(d));
        return absentDates.Count + extra;
    }
}
