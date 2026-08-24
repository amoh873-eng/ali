using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// سجل حضور يومي لموظف واحد في يوم تقويمي واحد.
/// لماذا سجل واحد لكل (موظف، تاريخ) فقط؟ لمنع ازدواجية البيانات وضمان أن
/// حساب الرواتب يعتمد على عدد أيام الغياب/الإجازة غير المدفوعة بشكل حتمي،
/// وليس على تكرارات عشوائية لنفس اليوم.
/// يُفرض التفرد عبر فهرس فريد على (EmployeeId, Date) في إعدادات EF.
/// </summary>
public class AttendanceRecord : BaseEntity
{
    /// <summary>
    /// المعرّف الفريد للموظف.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// الموظف المرتبط بهذا السجل.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// تاريخ الحضور (الجزء التقويمي فقط — يُخزّن كـ date في SQL Server).
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// حالة الحضور لذلك اليوم (حاضر/غائب/متأخر/في إجازة).
    /// </summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>
    /// وقت الحضور (اختياري).
    /// </summary>
    public TimeSpan? CheckInTime { get; set; }

    /// <summary>
    /// وقت الانصراف (اختياري).
    /// </summary>
    public TimeSpan? CheckOutTime { get; set; }

    /// <summary>
    /// ملاحظات إضافية (اختيارية).
    /// </summary>
    public string? Notes { get; set; }
}
