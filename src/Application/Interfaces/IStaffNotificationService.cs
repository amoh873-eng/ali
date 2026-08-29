using ERPSystem.Application.DTOs.Notifications;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// خدمة إشعارات الموظفين (الموجهة للمستخدمين العاديين).
/// منفصلة تماماً عن نظام إشعارات أصحاب النظام (NotificationDigestService).
/// يستخدمها الفحص الخلفي (نقص مخزون / انتهاء صلاحية) ويستعرضها جرس الواجهة.
/// </summary>
public interface IStaffNotificationService
{
    /// <summary>إنشاء إشعار جديد لمستخدم معين.</summary>
    Task CreateAsync(string userId, string title, string message, string? linkUrl,
        NotificationCategory category, string? dedupKey = null, CancellationToken ct = default);

    /// <summary>وضع إشعار كمقروء.</summary>
    Task MarkAsReadAsync(Guid id, CancellationToken ct = default);

    /// <summary>وضع كل إشعارات المستخدم كمقروء.</summary>
    Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);

    /// <summary>عدد الإشعارات غير المقروءة لمستخدم.</summary>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);

    /// <summary>أحدث إشعارات مستخدم (تنازلياً بالوقت).</summary>
    Task<List<NotificationDto>> GetRecentAsync(string userId, int count, CancellationToken ct = default);

    /// <summary>مازال هنالك إشعار غير مقروء (أو من الفترة المذكورة) بنفس مفتاح التكرار؟ (لمنع الازدواج يومياً).</summary>
    Task<bool> ExistsDedupAsync(string userId, NotificationCategory category, string dedupKey, DateTime sinceUtc, CancellationToken ct = default);
}