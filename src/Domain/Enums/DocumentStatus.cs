namespace ERPSystem.Domain.Enums;

/// <summary>
/// Lifecycle state of a business document (invoice / return).
/// - مسودة (Draft): يمكن تعديلها، لا تُنشئ قيوداً ولا حركات مخزون بعد.
/// - مرحّلة (Posted): حُوّلت نهائياً → نُشئ قيدها المحاسبي وحركة مخزونها.
/// - ملغاة (Cancelled): أُلغي المستند ولم يعد سارياً.
/// السبب في وجود مسودة: تتيح للمستخدم تحضير الفاتورة وحفظها دون التأثير
/// على المخزون أو الحسابات حتى يقرر "الترحيل" بشكل صريح.
/// </summary>
public enum DocumentStatus
{
    /// <summary>مسودة - Draft (لم تُرحّل بعد)</summary>
    Draft = 1,

    /// <summary>مرحّلة - Posted (نُشئ القيد وحركة المخزون)</summary>
    Posted = 2,

    /// <summary>ملغاة - Cancelled</summary>
    Cancelled = 3
}
