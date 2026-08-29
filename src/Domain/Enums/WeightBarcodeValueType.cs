namespace ERPSystem.Domain.Enums;

/// <summary>
/// نوع القيمة المدمجة في الباركود الصادر من ميزان الكتروني (وزن أو سعر).
/// </summary>
public enum WeightBarcodeValueType
{
    /// <summary>القيمة المدمجة هي الوزن (تُضرب بسعر الوحدة لحساب إجمالي البند).</summary>
    Weight = 0,

    /// <summary>القيمة المدمجة هي السعر النهائي للبند مباشرة (لا ضرب).</summary>
    Price = 1
}