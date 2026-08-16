namespace ERPSystem.Web.Components.Shared;

/// <summary>
/// شريحة واحدة في الرسم الدائري (Donut): تسمية + قيمة رقمية + لون + نص عرض منسّق.
/// </summary>
public class DonutSegment
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Color { get; set; } = "#6D5BD0";
    public string ValueText { get; set; } = string.Empty;
}
