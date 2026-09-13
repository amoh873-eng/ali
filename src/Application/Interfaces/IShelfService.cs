using ERPSystem.Application.DTOs.Shelf;

namespace ERPSystem.Application.Interfaces;

/// <summary>وحدة منسّق الرفوف: فحوصات، حدود رف، تحذيرات انتهاء الدُفعات، ورصد أسعار المنافسين.</summary>
public interface IShelfService
{
    /// <summary>حدود الرف (اختيارياً لصنف) للعرض على الجوال.</summary>
    Task<List<ShelfParLevelDto>> GetParLevelsAsync(Guid? itemId);

    /// <summary>يسجّل فحص رف (مع حفظ الصور الاختيارية base64 على القرص) ويعيده.</summary>
    Task<ShelfCheckDto> CreateShelfCheckAsync(CreateShelfCheckDto dto, string createdByUserId);

    /// <summary>سجل الفحوصات (الأحدث أولاً؛ اختيارياً صنف/موقع).</summary>
    Task<List<ShelfCheckDto>> GetShelfChecksAsync(Guid? itemId, string? location);

    /// <summary>تحذيرات الدُفعات القريبة من الانتهاء ضمن ExpiryWarningWindowDays (لصنف اختياري).</summary>
    Task<List<ShelfExpiryWarningDto>> GetExpiryWarningsAsync(Guid? itemId);

    /// <summary>يرصد سعر منافس (مع صورة اختيارية).</summary>
    Task<ShelfPriceCaptureDto> CreatePriceCaptureAsync(CreateShelfPriceCaptureDto dto, string createdByUserId);

    /// <summary>سجل رصد أسعار المنافسين (الأحدث أولاً).</summary>
    Task<List<ShelfPriceCaptureDto>> GetPriceCapturesAsync();

    /// <summary>تلخيص النشاط الميداني لكل مستخدم (فحوصات/رصد أسعار/تحصيلات) — لوحة المدير.</summary>
    Task<List<ShelfFieldActivityDto>> GetFieldActivityAsync();

    /// <summary>أصناف لُحظت تحت حد رفها في موقع مراتٍ متكررة — مؤشر مشكلة تزويد متكررة.</summary>
    Task<List<ShelfShortfallDto>> GetShortfallsAsync();
}