using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// إشعار موجّه للموظف (صاحب دور Inventory أو غيرهم) — يظهر في جرس الإشعارات أعلى الشاشة.
/// نظام منفصل تماماً عن NotificationDigestService (الذي يخص أصحاب النظام Vendor فقط).
/// يحمل نصاً جاهزاً (Title/Message) مع رابط اختياري للتنقّل عند النقر، وحالة قراءة.
/// </summary>
public class Notification
{
    /// <summary>المعرّف الفريد للإشعار.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// معرّف المستخدم المستلم (مفتاح أجنبي إلى AspNetUsers.Id من نوع string).
    /// </summary>
    public string RecipientUserId { get; set; } = string.Empty;

    /// <summary>عنوان الإشعار (نص جاهز مترجم/عربي).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>نص الإشعار التفصيلي (نص جاهز).</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>رابط اختياري يُفتح عند النقر على الإشعار (مثل صفحة الصنف/التقرير).</summary>
    public string? LinkUrl { get; set; }

    /// <summary>مفتاح داخلي لمنع التكرار (مثل "low-stock:{ItemId}" أو "expiry:{BatchId}") — ليس جزءاً من الواجهة.</summary>
    public string? DedupKey { get; set; }

    /// <summary>هل قُرئ الإشعار بعد؟</summary>
    public bool IsRead { get; set; }

    /// <summary>وقت إنشاء الإشعار.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>تصنيف الإشعار (نقص مخزون / تنبيه انتهاء صلاحية / نظام).</summary>
    public NotificationCategory Category { get; set; }
}