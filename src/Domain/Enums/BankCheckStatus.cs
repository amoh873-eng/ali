namespace ERPSystem.Domain.Enums;

/// <summary>
/// حالة الشيك خلال دورة حياته:
/// Registered (بانتظار الاستحقاق) → Deposited (أودع للتحصيل — للمستلم فقط، اختياري)
/// → Cleared (تُحصِّل/صُرف بنجاح) أو Bounced (ارتداد بسبب كفاية الرصيد أو غيره).
/// </summary>
public enum BankCheckStatus
{
    /// <summary>مُسجّل وبانتظار الاستحقاق (لم يُودع بعد / لم يُحصل).</summary>
    Registered = 1,

    /// <summary>أُودع للتحصيل (للمستلم فقط — مرحلة وسيطة اختيارية قبل التحصيل الفعلي).</summary>
    Deposited = 2,

    /// <summary>تَحصَّل/صُرف بنجاح — أصبح نقداً في البنك.</summary>
    Cleared = 3,

    /// <summary>ارتد (لا أموال كافية أو سبب آخر) — عُكس الأثر الأول.</summary>
    Bounced = 4
}