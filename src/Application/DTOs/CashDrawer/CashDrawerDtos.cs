using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.CashDrawer;

/// <summary>
/// نموذج إدخال تحويل نقد بين الخزنة الرئيسية ودرج الكاش.
/// الاتجاه (FloatIn/CashOut) يحدد اتجاه القيد المحاسبي.
/// </summary>
public class CreateCashTransferDto
{
    /// <summary>الاتجاه: تمويل درج الكاش (من الخزنة) أو سحب نقد (إلى الخزنة).</summary>
    public CashTransferDirection Type { get; set; }

    /// <summary>المبلغ المحوَّل (أكبر من صفر).</summary>
    public decimal Amount { get; set; }

    /// <summary>ملاحظة حرة اختيارية (مثال: بداية وردية الصباح / تحصين منتصف النهار).</summary>
    public string? Notes { get; set; }

    /// <summary>وقت العملية (افتراضياً الآن).</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>كائن عرض لتحويل نقدي مع بيانات الصلة (الكاشير والقيد).</summary>
public class CashDrawerTransactionDto
{
    public Guid Id { get; set; }
    public CashTransferDirection Type { get; set; }

    /// <summary>اسم عربي للاتجاه للعرض.</summary>
    public string TypeNameAr => Type == CashTransferDirection.FloatIn ? "تمويل درج الكاش" : "سحب نقد إلى الخزنة";

    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string CashierUserId { get; set; } = string.Empty;

    /// <summary>بريد/اسم أمين الصندوق الذي نفّذ التحويل.</summary>
    public string CashierName { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public Guid JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string EntryDescription { get; set; } = string.Empty;
}

/// <summary>مرشّحات سجل التحويلات (تاريخ / اتجاه / كاشير).</summary>
public class CashDrawerFilterDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public CashTransferDirection? Type { get; set; }
    public string? Cashier { get; set; }
}
