namespace ERPSystem.Domain.Enums;

/// <summary>
/// حالة طلب الإجازة (Leave status).
/// - Pending (معلّقة): بانتظار الموافقة أو الرفض.
/// - Approved (موافق عليها): تمت الموافقة على الطلب.
/// - Rejected (مرفوضة): تم رفض الطلب.
/// </summary>
public enum LeaveStatus
{
    /// <summary>معلّقة - Pending</summary>
    Pending = 1,

    /// <summary>موافق عليها - Approved</summary>
    Approved = 2,

    /// <summary>مرفوضة - Rejected</summary>
    Rejected = 3
}
