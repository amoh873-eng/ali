namespace ERPSystem.Application.DTOs.Items;

/// <summary>
/// بند مُختار لطباعة ملصق باركود (يُستمد من كيان الصنف القائم — لا كيان جديد).
/// </summary>
public class BarcodeLabelItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SalePrice { get; set; }
}

/// <summary>
/// خيارات توليد ملف PDF الخاص بملصقات الباركود.
/// </summary>
public class BarcodeLabelOptionsDto
{
    /// <summary>إظهار السعر على الملصق؟ (بعض المتاجر لا ترغب بذلك)</summary>
    public bool ShowPrice { get; set; }

    /// <summary>عدد النسخ لكل بند (القيمة الشائعة عند تعبئة "نسخة لكل المحدد").</summary>
    public int CopiesPerItem { get; set; } = 1;
}