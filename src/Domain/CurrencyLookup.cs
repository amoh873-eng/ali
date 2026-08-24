// ── جدول رموز العملات للعرض فقط (لا يُستخدم في أي حساب تحويل) ──
// كل سطر: رمز ISO → (رمز العرض). الأسماء مترجمة عبر resx بـ Loc["Currency_JOD"].
// لماذا قاموس ثابت وليس جدول DB؟ لأنه بيانات عرض ثابتة، لا معاملات.
// لماذا لا تحويل؟ النظام بعملة واحدة فقط — تغييرها يغير التسمية فقط.
namespace ERPSystem.Domain;
public static class CurrencyLookup
{
    // الرمز فقط — الاسم يُجلب من Loc["Currency_XXX"] (عربي/إنجليزي).
    // هذه مجرد بيانات عرض، لا تُستخدم لأي حساب تحويل (لا يوجد تعدد عملات).
    public static readonly IReadOnlyDictionary<string, string> Symbols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["JOD"] = "د.أ", // دينار أردني — العملة الافتراضية للنظام
        ["SAR"] = "ر.س", // ريال سعودي — عملة خليجية شائعة
        ["AED"] = "د.إ", // درهم إماراتي
        ["USD"] = "$",   // دولار أمريكي — مرجع عالمي
        ["EUR"] = "€",   // يورو — للتعامل الأوروبي
        ["KWD"] = "د.ك", // دينار كويتي
        ["BHD"] = "د.ب", // دينار بحريني
        ["OMR"] = "ر.ع", // ريال عماني
        ["QAR"] = "ر.ق", // ريال قطري
        ["EGP"] = "ج.م", // جنيه مصري
        ["GBP"] = "£",   // جنيه إسترليني
        ["TRY"] = "₺",   // ليرة تركية
    };
    // إرجاع الرمز للكود، أو الكود نفسه كاحتياط إذا غير معروف.
    public static string SymbolFor(string? code) => string.IsNullOrWhiteSpace(code) ? "د.أ" : Symbols.TryGetValue(code.Trim(), out var s) ? s : code.Trim();
}
