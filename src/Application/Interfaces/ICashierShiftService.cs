using ERPSystem.Application.DTOs.Shifts;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// عقد خدمة ورديات الكاشير و تسوية درج الكاش: فتح وردية (برصيد فكة مرتبط بـ Float-In)،
/// حساب الرصيد المتوقَّع منعزلاً، إغلاق بتسوية (مع قيد فروقات إن وُجد فرق)، وتاريخ الورديات.
/// </summary>
public interface ICashierShiftService
{
    /// <summary>فتح وردية للكاشير: يتحقق من عدم وجود وردية مفتوحة، ينشئ Float-In عنده، ويُسجّل الوردية.</summary>
    Task<CashierShiftDto> StartAsync(decimal openingFloatAmount, string cashierUserId);

    /// <summary>الوردية المفتوحة الحالية للمستخدم (أو null).</summary>
    Task<CashierShiftDto?> GetOpenShiftAsync(string cashierUserId);

    /// <summary>حساب ملخّص مصادر المعادلة لوردية محددة (نطاق الكاشير والزمن فقط).</summary>
    Task<ShiftSummaryDto> ComputeSummaryAsync(Guid shiftId);

    /// <summary>إغلاق الوردية: يحسب المتوقَّع، يثبّت المعدود/الفرق، ويُرحّل قيد «فروقات الصندوق» إن وُجد فرق.</summary>
    Task<CashierShiftDto> CloseAsync(Guid shiftId, decimal countedCashAmount, string? notes, string closedByUserId);

    /// <summary>سجل الورديات (المغلقة) مع فلترة الكاشير/التاريخ — صلاحية إدارية.</summary>
    Task<List<CashierShiftDto>> GetHistoryAsync(CashierShiftFilterDto filter);
}