namespace ERPSystem.Domain.Enums;

/// <summary>
/// تصنيف إشعارات الموظفين (الموجهة للمستخدمين العاديين، وليست لأصحاب النظام).
/// </summary>
public enum NotificationCategory
{
    /// <summary>نقص مخزون صنف تحت الحد الأدنى.</summary>
    LowStock = 0,

    /// <summary>تحذير من قرب/تجاوز تاريخ انتهاء صلاحية دفعة.</summary>
    ExpiryWarning = 1,

    /// <summary>إشعار نظام عام.</summary>
    System = 2
}