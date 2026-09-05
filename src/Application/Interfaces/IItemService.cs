using ERPSystem.Application.DTOs.Items;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for inventory item operations (الأصناف).
/// </summary>
public interface IItemService
{
    /// <summary>
    /// Returns all active items with category and unit names.
    /// </summary>
    Task<List<ItemDto>> GetAllAsync();

    /// <summary>
    /// بحث في قاعدة البيانات بمطابقة ILike (غير حساسة لحالة الأحرف — PostgreSQL)
    /// على الكود والاسم (عربي/إنجليزي) والباركود. يُستخدم في شاشة الأصناف بدل فلترة الذاكرة
    /// لأن LIKE/Contains على مستوى SQL في PostgreSQL حساس لحالة الأحرف.
    /// </summary>
    Task<List<ItemDto>> SearchAsync(string term);

    /// <summary>
    /// Returns items whose current stock is below the minimum level.
    /// </summary>
    Task<List<ItemDto>> GetLowStockAsync();

    /// <summary>
    /// Gets a single item by id.
    /// </summary>
    Task<ItemDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new item. Validates code uniqueness, category and unit existence.
    /// </summary>
    Task<ItemDto> CreateAsync(CreateItemDto dto);

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    Task<ItemDto> UpdateAsync(UpdateItemDto dto);

    /// <summary>
    /// Soft-deletes an item. Fails for system items or items with movements.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggles the active status of an item.
    /// </summary>
    Task ToggleActiveAsync(Guid id);

    // ────────────────────────── الاستيراد الجماعي ──────────────────────────

    /// <summary>
    /// يقرأ ملفاً جدولياً (xlsx / csv / txt) ويعيد ترويساته وصفوفه الخام.
    /// يرمي InvalidOperationException برسالة عربية واضحة عند صيغة غير مدعومة أو ملف تالف.
    /// </summary>
    Task<ParsedImportFileDto> ParseImportFileAsync(Stream fileStream, string fileName);

    /// <summary>
    /// يفسّر الصفوف حسب تعيين الأعمدة ويفحصها (اسم/باركود إلزامي + تكرار + موجودات + أسعار)
    /// دون كتابة أي شيء في قاعدة البيانات. يعيد ملخصاً + أول 100 صف.
    /// </summary>
    Task<ItemImportPreviewDto> BuildImportPreviewAsync(ParsedImportFileDto file, BulkImportColumnMapDto map);

    /// <summary>
    /// ينفذ الاستيراد الفعلي بدفعات (500 صف) مع توليد أكواد دفعة واحدة وتقدّم قابل للملاحظة.
    /// الباركود الموجود مسبقاً يُتخطى بلا تعديل (القرار المعتمد).
    /// </summary>
    Task<ItemImportResultDto> ExecuteBulkImportAsync(ParsedImportFileDto file, BulkImportColumnMapDto map, Action<int, int>? progress = null);

    /// <summary>ينشئ ملف Excel (xlsx) بصفوف الفشل/التخطّي وأسبابها للتنزيل وإعادة الاستيراد.</summary>
    byte[] BuildImportFailuresReport(List<ItemImportFailureDto> failures);

    /// <summary>
    /// يُعيد الأصناف المنشأة ضمن "آخر دفعة استيراد جماعي" (لا شيء إن لم تكن دفعة).
    /// تُستخدم في شاشة ملصقات الباركود لاختيارها بضغطة واحدة بعد استيراد آلاف الأصناف.
    /// </summary>
    Task<List<ItemDto>> GetItemsFromLastBulkImportAsync();
}