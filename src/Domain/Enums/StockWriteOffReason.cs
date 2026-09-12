namespace ERPSystem.Domain.Enums;

/// <summary>
/// أسباب إتلاف المخزون (قائمة ثابتة لسند الإتلاف StockWriteOff).
/// عند اختيار "أخرى" يُطلب حقل نص حر إضافي (OtherReasonText).
/// </summary>
public enum StockWriteOffReason
{
    /// <summary>منتهي الصلاحية</summary>
    Expired = 1,

    /// <summary>تالف/مكسور</summary>
    Damaged = 2,

    /// <summary>سرقة أو فقد</summary>
    Stolen = 3,

    /// <summary>أخرى — مع حقل نص حر إضافي</summary>
    Other = 4
}