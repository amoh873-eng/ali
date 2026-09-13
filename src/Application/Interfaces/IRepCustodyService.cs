using ERPSystem.Application.DTOs.RepCustody;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// عهدة نقدية بيد مندوبي المبيعات: تحصيل ميداني من العملاء وتسليمه لاحقاً للمكتب.
/// حساب محاسبي واحد «عهدة نقدية بيد المندوبين» (1110) مع بُعد تحليلي بـ RepUserId —
/// كل عملية ترحّل قيداً محاسبياً متوازناً ذرياً (نفس أسلوب CashDrawer/SalesInvoice):
///   Collect : مدين 1110 / دائن «ذمم العملاء» 1200
///   Delivery: مدين «الصندوق» 1100 / دائن 1110
/// </summary>
public interface IRepCustodyService
{
    /// <summary>تحصيل ميداني (أو تسليم للمكتب حسب dto.Effect) وترحيل ذري.</summary>
    Task<RepCustodyDto> CreateAsync(CreateRepCustodyDto dto, string actorUserId);

    /// <summary>رصيد العهدة الحالي لكل مندوب (Σ التحصيل − Σ التسليم).</summary>
    Task<List<RepCustodyBalanceDto>> GetBalancesAsync();

    /// <summary>سجل العمليات (الأحدث أولاً) مع الفلترة.</summary>
    Task<List<RepCustodyDto>> GetAsync(RepCustodyFilterDto filter);
}