namespace ERPSystem.Domain.Enums;

/// <summary>
/// حالة الموظف (Employee status) — دورة حياة الموظف داخل المنشأة.
/// - Active (نشط): يعمل حالياً ويُسحَب تلقائياً في أي معالجة رواتب مستقبلية.
/// - Suspended (موقوف): موقوف مؤقتاً عن العمل.
/// - Terminated (منتهي الخدمة): انتهت خدمته نهائياً.
/// لا نستخدم IsActive هنا لأن الحالة ثلاثية وليست ثنائية.
/// </summary>
public enum EmployeeStatus
{
    /// <summary>نشط - Active</summary>
    Active = 1,

    /// <summary>موقوف - Suspended</summary>
    Suspended = 2,

    /// <summary>منتهي الخدمة - Terminated</summary>
    Terminated = 3
}
