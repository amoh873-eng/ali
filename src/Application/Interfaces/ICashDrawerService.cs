using ERPSystem.Application.DTOs.CashDrawer;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// تحويلات النقد بين الخزنة الرئيسية «الصندوق» (1100) ودرج الكاش (1105).
///
/// هذه العمليات ليست مصروفاً ولا إيراداً — نقل نفس المال بين موقعين نقديين،
/// وكل تحويل يرحّل قيداً محاسبياً متوازناً في معاملة واحدة ذرية:
///   FloatIn : مدين «درج الكاش» 1105 / دائن «الصندوق» 1100
///   CashOut : مدين «الصندوق» 1100 / دائن «درج الكاش» 1105
/// </summary>
public interface ICashDrawerService
{
    /// <summary>
    /// ينفّذ تحويلاً نقدياً ويرحّله ذرياً: يسجّل العملية + القيد المحاسبي المتوازن في نفس SaveChanges.
    /// </summary>
    Task<CashDrawerTransactionDto> CreateAsync(CreateCashTransferDto dto, string cashierUserId);

    /// <summary>سجل التحويلات مع الفلترة حسب الفترة / الاتجاه / الكاشير (الأحدث أولاً).</summary>
    Task<List<CashDrawerTransactionDto>> GetAsync(CashDrawerFilterDto filter);

    /// <summary>تحويل واحد مع بيانات القيد المرتبط.</summary>
    Task<CashDrawerTransactionDto?> GetByIdAsync(Guid id);
}
