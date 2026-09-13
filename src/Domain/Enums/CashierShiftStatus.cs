namespace ERPSystem.Domain.Enums;

/// <summary>
/// حالة وردية الكاشير: مفتوحة (قيد العمل — بلا EndTime) أو مغلقة (نُفّذت التسوية).
/// </summary>
public enum CashierShiftStatus
{
    /// <summary>مفتوحة — الصندوق فعال ولا يمكن فتح وردية ثانية للكاشير نفسه.</summary>
    Open = 1,

    /// <summary>مغلقة — أُدخل العدد الفعلي وسُجّل الفرق إن وُجد (وتُحسب الأرصدة نهائياً).</summary>
    Closed = 2
}