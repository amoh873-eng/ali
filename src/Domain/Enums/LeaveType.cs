namespace ERPSystem.Domain.Enums;

/// <summary>
/// نوع الإجازة (Leave type) — نسخة مبسّطة لا تغني عن نظام حضور/انصراف منفصل.
/// </summary>
public enum LeaveType
{
    /// <summary>سنوية - Annual</summary>
    Annual = 1,

    /// <summary>مرضية - Sick</summary>
    Sick = 2,

    /// <summary>بدون راتب - Unpaid</summary>
    Unpaid = 3,

    /// <summary>طارئة - Emergency</summary>
    Emergency = 4
}
