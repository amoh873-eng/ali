using ERPSystem.Application.DTOs.Inventory;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// عمليات سندات الإتلاف المخزني (Stock Write-Off):
/// إنشاء سند يخصم المخزون ويرحّل قيداً محاسبياً متوازناً في معاملة واحدة ذرية،
/// عرض السندات وفلترتها، دُفعات الاختيار، والتقرير الملخّص (إجمالي الهالك حسب السبب/الفترة).
/// </summary>
public interface IStockWriteOffService
{
    /// <summary>
    /// ينشئ سند إتلاف ويرحّله ذرياً: خصم الكمية من المخزون (من دُفعة محددة إن تتبّع الصنف دُفعات)
    /// + قيد محاسبي (مدين مصروف الهالك / دائن المخزون) في نفس SaveChanges.
    /// يعيد السند بعد حفظه مع بيانات القيد المرتبط.
    /// </summary>
    Task<StockWriteOffDto> CreateAsync(CreateStockWriteOffDto dto, string createdByUserId);

    /// <summary>السندات مع الفلترة حسب الصنف والسبب والفترة (الأحدث أولاً).</summary>
    Task<List<StockWriteOffDto>> GetAsync(Guid? itemId, int? reason, DateTime? from, DateTime? to);

    /// <summary>سند واحد مع الصلات (صنف/مخزن/دُفعة/قيد).</summary>
    Task<StockWriteOffDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// دُفعات صنف في مخزن (بكميات موجبة) لعرضها في نموذج السند — مرتبة الأقرب انتهاءً أولاً.
    /// الدُفعات المنتهية تُعرض أيضاً (الإتلاف غالباً يخصّها).
    /// </summary>
    Task<List<ItemBatchOptionDto>> GetBatchesForAsync(Guid itemId, Guid warehouseId);

    /// <summary>التقرير الملخّص: إجمالي القيمة/الكمية/العدد + تفصيل حسب السبب + توزيع شهري للفترة.</summary>
    Task<StockWriteOffSummaryDto> GetSummaryAsync(Guid? itemId, int? reason, DateTime? from, DateTime? to);
}