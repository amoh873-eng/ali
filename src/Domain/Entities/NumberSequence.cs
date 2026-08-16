namespace ERPSystem.Domain.Entities;

/// <summary>
/// جدول العدّادات التسلسلية (Number Sequences).
/// يُستخدم لتوليد أرقام المستندات والقيود بشكل آمن ضد التزامن،
/// بدلاً من نمط CountAsync() + 1 الذي يسبب تكرار الأرقام
/// عند إنشاء أكثر من مستند/قيد في نفس الطلب أو من مستخدمين متزامنين.
/// </summary>
public class NumberSequence
{
    public Guid Id { get; set; }

    /// <summary>مفتاح العدّاد (مثال: "JE-20260814" — البادئة + التاريخ).</summary>
    public string SequenceKey { get; set; } = string.Empty;

    /// <summary>آخر رقم تم إصداره لهذا المفتاح.</summary>
    public long LastValue { get; set; }
}
