using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// تحويل نقدي بين موقعين نقديين: الخزنة الرئيسية «الصندوق» (1100) ↔ درج الكاش (1105).
///
/// هذه العملية ليست مصروفاً ولا إيراداً ولا سحباً من المالك — هي نقل نفس المال من حساب نقدي
/// إلى حساب نقدي آخر:
///   - FloatIn  (الخزنة ← درج الكاش): بداية الوردية لإعطاء الكاشير فكة كافية.
///   - CashOut  (درج الكاش ← الخزنة): نهاية الوردية أو تحصين مبلغ متراكم.
///
/// يرافق كل تحويل قيد محاسبي متوازن في معاملة واحدة ذرية:
///   FloatIn : مدين «درج الكاش» 1105 / دائن «الصندوق» 1100
///   CashOut : مدين «الصندوق» 1100 / دائن «درج الكاش» 1105
/// </summary>
public class CashDrawerTransaction
{
    public Guid Id { get; set; }

    /// <summary>الاتجاه: تمويل درج الكاش (من الخزنة) أو سحب نقد (إلى الخزنة).</summary>
    public CashTransferDirection Type { get; set; }

    /// <summary>المبلغ المحوَّل (أكبر من صفر).</summary>
    public decimal Amount { get; set; }

    /// <summary>وقت العملية (محلي).</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>معرّف أمين الصندوق الذي نفّذ التحويل (مفتاح أجنبي إلى AspNetUsers.Id).</summary>
    public string CashierUserId { get; set; } = string.Empty;

    /// <summary>ملاحظة حرة اختيارية (مثال: «بداية وردية الصباح» أو «تحصين منتصف النهار»).</summary>
    public string? Notes { get; set; }

    /// <summary>معرّف القيد المحاسبي المرتبط بالتحويل (لتتبّع الأثر المحاسبي).</summary>
    public Guid JournalEntryId { get; set; }

    /// <summary>القيد المحاسبي المرتبط.</summary>
    public JournalEntry? JournalEntry { get; set; }

    public DateTime CreatedAt { get; set; }
}
