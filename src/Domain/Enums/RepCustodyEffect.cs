namespace ERPSystem.Domain.Enums;

/// <summary>
/// أثر العملية على عهدة المندوب النقدية:
/// Collect — تحصيل ميداني (يرفع رصيد العهدة)، Delivery — تسليم للمكتب (يخفض رصيد العهدة).
/// </summary>
public enum RepCustodyEffect
{
    /// <summary>تحصيل ميداني من عميل: مدين «عهدة نقدية بيد المندوبين» / دائن «ذمم العملاء».</summary>
    Collect = 1,

    /// <summary>تسليم المبلغ للخزنة الرئيسية (1100): مدين «الصندوق» / دائن «عهدة نقدية بيد المندوبين» — يُصفِّر العهدة.</summary>
    Delivery = 2
}