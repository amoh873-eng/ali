namespace ERPSystem.Web.Components.Shared;

/// <summary>
/// سلسلة بيانات في مخطط الاتجاه (خط/مساحة): اسم + لون + قيم رقمية متتالية.
/// </summary>
public class TrendSeries
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6D5BD0";
    public List<double> Values { get; set; } = new();
}
