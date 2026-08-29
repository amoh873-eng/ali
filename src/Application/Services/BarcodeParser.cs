using System.Text.Json;
using System.Text.Json.Serialization;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Services;

/// <summary>
/// قاعدة قابلة للضبط لتحليل الباركود الصادر من ميزان الكتروني يبيع صنفاً بالوزن.
///
/// الميزان يطبع باركوداً يتضمن: بادئة ثابتة (عادةً "20"-"29" GS1 للاستخدام داخل
/// المتجر) ثم رمز الصنف ثم قيمة (وزن أو سعر) بفاصلة عشرية ضمنية ثم خانة تحقق.
/// تنسيق المواضع يختلف بين ماركات الموازين — لذا لا نصلّب صيغة واحدة، بل نقرأ
/// هذه القاعدة القابلة للضبط من SystemSettings (WeightBarcodeRuleJson).
///
/// !! تحذير تطبيقي (مثل تحذير JoFotara): إن ضُبط تنسيق المواضع خطأً،
/// سيُحسب سعر كل بيع موزون خطأً بصمت. تحقّق من تنسيق باركود جهاز الميزان
/// الفعلي لدى العميل/الشركة المورّدة قبل اعتماد القاعدة النهائية للنشر الحقيقي.
/// </summary>
public class WeightBarcodeRule
{
    /// <summary>بادئة ثابتة في بداية الباركود للتمييز (مثال "24").</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>موضع بداية رمز الصنف (فهرس صفري).</summary>
    public int ItemCodeStart { get; set; }

    /// <summary>طول رمز الصنف بعد موضع البداية.</summary>
    public int ItemCodeLength { get; set; }

    /// <summary>موضع بداية القيمة (وزن أو سعر) (فهرس صفري).</summary>
    public int ValueStart { get; set; }

    /// <summary>طول القيمة (عدد الخانات).</summary>
    public int ValueLength { get; set; }

    /// <summary>هل القيمة وزن أم سعر نهائي.</summary>
    public WeightBarcodeValueType ValueType { get; set; } = WeightBarcodeValueType.Weight;

    /// <summary>عدد الخانات العشرية الضمنية (الفاصلة المنزلية) في القيمة.</summary>
    public int DecimalPlaces { get; set; } = 3;

    public string ToJson() => JsonSerializer.Serialize(this);
    public static WeightBarcodeRule? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<WeightBarcodeRule>(json); }
        catch { return null; }
    }

    /// <summary>
    /// مثال توضيحي (وليس افتراضياً مفعّلاً): GS1 داخل المتجر — بادئة "24"،
    /// رمز الصنف من الموضع 2 بطول 4، وزن 5 خانات بفاصلة عشرية واحدة.
    /// لا يُفعَّل افتراضياً؛ انسخ منه عند ضبط إعداد عميل فعلي.
    /// </summary>
    public static WeightBarcodeRule SampleGs1Weight()
        => new()
        {
            Prefix = "24",
            ItemCodeStart = 2,
            ItemCodeLength = 4,
            ValueStart = 6,
            ValueLength = 5,
            ValueType = WeightBarcodeValueType.Weight,
            DecimalPlaces = 3
        };
}

/// <summary>نتيجة تحليل باركود موزون.</summary>
public class WeightBarcodeResult
{
    /// <summary>رمز الصنف المستخرج (يُطابق به Item.Code عادة).</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>القيمة المستخرجة (وزن أو سعر حسب القاعدة) بعد تطبيق الفاصلة.</summary>
    public decimal Value { get; set; }

    /// <summary>نوع القيمة المستخرجة.</summary>
    public WeightBarcodeValueType ValueType { get; set; }

    /// <summary>الكود الأصلي كما مُسح.</summary>
    public string RawCode { get; set; } = string.Empty;
}

/// <summary>
/// محلّل الباركود الموزون — يقرر هل المسح "باركود ميزان موزون" أم "باركود عادي".
/// </summary>
public static class BarcodeParser
{
    /// <summary>
    /// يحاول تحليل code وفق القاعدة. يعيد false إذا لم يكن باركوداً موزوناً
    /// (يجب عندها الرجوع للبحث العادي بحقل Item.Barcode).
    /// </summary>
    public static bool TryParseWeightEmbedded(string code, WeightBarcodeRule? rule, out WeightBarcodeResult? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(code)) return false;
        if (rule is null) return false;

        var raw = code.Trim();

        if (!raw.StartsWith(rule.Prefix, StringComparison.Ordinal)) return false;

        // أبعاد كافية؟
        if (rule.ItemCodeStart < 0 || rule.ItemCodeLength <= 0) return false;
        if (rule.ItemCodeStart + rule.ItemCodeLength > raw.Length) return false;
        if (rule.ValueLength > 0 && rule.ValueStart + rule.ValueLength > raw.Length) return false;

        var itemCode = raw.Substring(rule.ItemCodeStart, rule.ItemCodeLength);

        decimal value = 0m;
        if (rule.ValueLength > 0)
        {
            var valueDigits = raw.Substring(rule.ValueStart, rule.ValueLength);
            if (!decimal.TryParse(valueDigits, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                return false;

            // تطبيق الفاصلة العشرية الضمنية.
            if (rule.DecimalPlaces > 0)
                value = parsed / (decimal)Math.Pow(10, rule.DecimalPlaces);
            else
                value = parsed;
        }

        result = new WeightBarcodeResult
        {
            ItemCode = itemCode,
            Value = value,
            ValueType = rule.ValueType,
            RawCode = raw
        };
        return true;
    }
}