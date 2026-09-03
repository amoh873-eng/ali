namespace ERPSystem.Application.DTOs.Items;

/// <summary>
/// ملف جدولي مُحلَّل بعد الرفع: الترويسات + صفوف البيانات الخام (بدون الترويسة).
/// </summary>
public class ParsedImportFileDto
{
    public string FileName { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public int TotalRows => Rows.Count;

    /// <summary>
    /// اقتراح تعيين الأعمدة (يُحسب في الخادم من أسماء ترويسات معروفة).
    /// حقلا الاسم والباركود مضمونان في الاقتراح (عمود 0 / 1 افتراضياً).
    /// </summary>
    public BulkImportColumnMapDto? SuggestedMap { get; set; }
}

/// <summary>
/// تعيين أعمدة الملف إلى حقول النظام (فهرسة صفرية داخل صف الملف؛ -1 = غير مسدَّس).
/// الاسم والباركود إلزاميان — والباقي اختياري يُترك -1 طوعاً.
/// </summary>
public class BulkImportColumnMapDto
{
    public int NameColumn { get; set; } = -1;
    public int BarcodeColumn { get; set; } = -1;
    public int? CostColumn { get; set; }
    public int? SaleColumn { get; set; }
    public int? CategoryColumn { get; set; }
    public int? UnitColumn { get; set; }

    /// <summary>هل حقلان الإلزاميان مسدَّسان؟</summary>
    public bool IsComplete => NameColumn >= 0 && BarcodeColumn >= 0;

    public static readonly int NotMapped = -1;
}

/// <summary>حالة الصف بعد المعاينة (لا تُكتب لقاعدة البيانات في هذه المرحلة).</summary>
public enum ItemImportRowStatus
{
    /// <summary>صالح للاستيراد المباشر</summary>
    Ready,

    /// <summary>اسم الصنف مفقود — رفض</summary>
    MissingName,

    /// <summary>الباركود مفقود — رفض</summary>
    MissingBarcode,

    /// <summary>باركود مكرر داخل الملف نفسه — رفض</summary>
    DuplicateInFile,

    /// <summary>الباركود موجود مسبقاً في النظام — سيُتخطى (الخيار الآمن المعتمد)</summary>
    ExistsInDatabase,

    /// <summary>قيمة سعر الشراء غير صالحة — رفض</summary>
    InvalidCost,

    /// <summary>قيمة سعر البيع غير صالحة — رفض</summary>
    InvalidSale,

    /// <summary>سعري الشراء والبيع معاً غير صالحين — رفض</summary>
    InvalidCostAndSale
}

/// <summary>
/// صف معروض في جدول المعاينة (أول 100 صف) مع نتيجة التفسير والتحقق.
/// </summary>
public class ItemImportPreviewRowDto
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal? CostPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string CategoryInput { get; set; } = string.Empty;
    public string UnitInput { get; set; } = string.Empty;
    public string CategoryMatchedTo { get; set; } = string.Empty;
    public string UnitMatchedTo { get; set; } = string.Empty;

    /// <summary>أسباب رفض الصف (نصوص عربية واضحة).</summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>ملاحظات إرشادية لا تمنع الاستيراد (فئة/وحدة افتراضية).</summary>
    public List<string> Notices { get; set; } = new();

    public ItemImportRowStatus Status { get; set; }

    public string StatusNameAr => Status switch
    {
        ItemImportRowStatus.Ready => "جاهز للاستيراد",
        ItemImportRowStatus.MissingName => "اسم الصنف مفقود",
        ItemImportRowStatus.MissingBarcode => "الباركود مفقود",
        ItemImportRowStatus.DuplicateInFile => "باركود مكرر داخل الملف",
        ItemImportRowStatus.ExistsInDatabase => "موجود في النظام — سيُتخطى",
        ItemImportRowStatus.InvalidCost => "سعر شراء غير صالح",
        ItemImportRowStatus.InvalidSale => "سعر بيع غير صالح",
        _ => "سعران غير صالحين"
    };
}

/// <summary>
/// نتيجة مرحلة المعاينة — لا تُكتَب أي بيانات في قاعدة البيانات.
/// </summary>
public class ItemImportPreviewDto
{
    public int TotalRows { get; set; }
    public int ValidCount { get; set; }
    public int ExistsInDbCount { get; set; }
    public int DuplicateInFileCount { get; set; }
    public int ErrorCount { get; set; }
    public int NoticeCount { get; set; }

    /// <summary>عينة العرض: أول 100 صف.</summary>
    public List<ItemImportPreviewRowDto> SampleRows { get; set; } = new();
}

/// <summary>صف فشل/تخطٍّ في النتيجة النهائية (للتنزيل والإصلاح وإعادة الاستيراد).</summary>
public class ItemImportFailureDto
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>النتيجة النهائية بعد التنفيذ الفعلي.</summary>
public class ItemImportResultDto
{
    public int TotalRows { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedExistingCount { get; set; }
    public int FailedCount { get; set; }
    public int NoticeCount { get; set; }
    public long ElapsedMs { get; set; }
    public List<ItemImportFailureDto> Failures { get; set; } = new();
}