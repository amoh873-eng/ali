namespace ERPSystem.Domain.Enums;

/// <summary>
/// حالة الحضور اليومي للموظف (Attendance status).
/// - Present (حاضر): حضر الموظف في هذا اليوم.
/// - Absent (غائب): لم يحضر وليس لديه إجازة معتمدة تغطي اليوم.
/// - Late (متأخر): حضر متأخراً — يُسجّل للمتابعة ولا يخصم من الراتب تلقائياً.
/// - OnLeave (في إجازة): يوم مغطى بإجازة معتمدة — يُعامل كـ OnLeave سواء كانت مدفوعة أو غير مدفوعة
///   (قرار الخصم يُتخذ في وحدة الرواتب عبر نوع الإجازة LeaveType.Unpaid).
/// </summary>
public enum AttendanceStatus
{
    /// <summary>حاضر - Present</summary>
    Present = 1,

    /// <summary>غائب - Absent</summary>
    Absent = 2,

    /// <summary>متأخر - Late</summary>
    Late = 3,

    /// <summary>في إجازة - On Leave</summary>
    OnLeave = 4
}
