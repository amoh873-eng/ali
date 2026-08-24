namespace ERPSystem.Application.DTOs.Reports;
public class GenericReportTable
{
    public string TitleAr { get; set; } = "";
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public List<string>? Totals { get; set; }
    public string ParamSummary { get; set; } = "";
    public string? FooterNote { get; set; }
    public bool Landscape { get; set; } = true;
}
