using ERPSystem.Application.DTOs.BankChecks;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// عقد خدمة الشيكات البنكية: تسجيل واستحقاق متدرج (إيداع/تحصيل/ارتداد) + القوائم + تقرير الاستحقاق.
/// كل تغيير حالة يرحّل قيداً محاسبياً متوازناً في معاملة واحدة ذرّية.
/// </summary>
public interface IBankCheckService
{
    /// <summary>تسجيل شيك مستقل (غير مرتبط بفاتورة) مع فتح القيد «برسم...» فوراً.</summary>
    Task<BankCheckDto> RegisterAsync(CreateBankCheckDto dto);

    /// <summary>إيداع شيك مستلم للتحصيل (مرحلة وسيطة — بلا أثر محاسبي).</summary>
    Task<BankCheckDto> DepositAsync(Guid id, DateTime depositDate);

    /// <summary>تحصيل/صرف الشيك بنجاح — يتحول نقداً في البنك (1101).</summary>
    Task<BankCheckDto> ClearAsync(Guid id, DateTime clearedDate, string? notes = null);

    /// <summary>ارتداد الشيك — يُعكس الأثر الأول ويعود الدين كما كان.</summary>
    Task<BankCheckDto> BounceAsync(Guid id, DateTime bouncedDate, string? notes = null);

    /// <summary>قائمة الشيكات (اختياري بالاتجاه).</summary>
    Task<List<BankCheckDto>> ListAsync(BankCheckDirection? direction = null);

    /// <summary>تقرير الاستحقاق: مجاميع وارد/صادر + توقعات شهرية + المتأخرة.</summary>
    Task<BankChecksReportDto> ReportAsync(
        DateTime? from, DateTime? to, BankCheckDirection? direction, BankCheckStatus? status);
}