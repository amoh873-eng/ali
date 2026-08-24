using ERPSystem.Application.DTOs.Reports;
namespace ERPSystem.Application.Services;
public partial class ReportService
{
    public byte[] ExportReportExcel(GenericReportTable t)
    {
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("Report");
        for(int i=0;i<t.Headers.Count;i++) ws.Cell(1,i+1).Value = t.Headers[i];
        ws.Row(1).Style.Font.Bold = true;
        for(int r=0;r<t.Rows.Count;r++) for(int c=0;c<t.Headers.Count;c++) ws.Cell(r+2,c+1).Value = t.Rows[r].ElementAtOrDefault(c)??"";
        if(t.Totals!=null){var r=t.Rows.Count+2;for(int c=0;c<t.Totals.Count;c++){ws.Cell(r,c+1).Value=t.Totals[c]; ws.Cell(r,c+1).Style.Font.Bold=true;}}
        ws.Columns().AdjustToContents();
        using var ms=new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }
}
