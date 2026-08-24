namespace ERPSystem.Domain.Enums;

/// <summary>
/// حالة دورة الرواتب (Payroll run lifecycle).
/// - Draft (مسودة): قابلة للتعديل — يمكن تعديل OtherDeductions/OtherAllowances لكل سطر
///   وإعادة حساب NetPay وإجماليات الدورة قبل الاعتماد.
/// - Approved (معتمدة): المبالغ مُقفلة (amounts locked) لكن لم تُرحّل محاسبياً بعد.
///   لا يُسمح بتعديل البنود أو إعادة التوليد لنفس الفترة.
/// - Paid (مرحّلة/مدفوعة): تم إنشاء قيد محاسبي عبر IJournalEntryService.PrepareEntryAsync
///   وتم حفظ JournalEntryId. الدورة مُقفلة تماماً — لا تعديل ولا ترحيل ثانٍ.
/// لماذا ثلاث حالات وليس حالتين؟ لأن الاعتماد قرار إداري (HR يوافق على المبالغ)
/// بينما الترحيل قرار محاسبي (إنشاء القيد في دفتر اليومية). الفصل يسمح بمراجعة
/// المبالغ قبل أن تلمس دفاتر المحاسبة.
/// </summary>
public enum PayrollRunStatus
{
    /// <summary>مسودة - Draft (editable)</summary>
    Draft = 1,

    /// <summary>معتمدة - Approved (amounts locked, not yet posted)</summary>
    Approved = 2,

    /// <summary>مرحّلة/مدفوعة - Paid (journal entry posted, fully locked)</summary>
    Paid = 3
}
