using ClosedXML.Excel;
using ERPSystem.Application.DTOs.Reports;

namespace ERPSystem.Web.Services;

/// <summary>
/// يولّد ملفات Excel (xlsx) من بيانات التقارير المالية باستخدام ClosedXML.
/// </summary>
public static class ReportExporter
{
    public const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] ExportTrialBalance(TrialBalanceDto dto)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("ميزان المراجعة");
        ws.RightToLeft = true;

        ws.Cell(1, 1).Value = "الكود";
        ws.Cell(1, 2).Value = "الحساب";
        ws.Cell(1, 3).Value = "مدين";
        ws.Cell(1, 4).Value = "دائن";
        ws.Cell(1, 5).Value = "الرصيد";

        var row = 2;
        foreach (var line in dto.Lines)
        {
            ws.Cell(row, 1).Value = line.AccountCode;
            ws.Cell(row, 2).Value = line.AccountNameAr;
            ws.Cell(row, 3).Value = line.TotalDebit;
            ws.Cell(row, 4).Value = line.TotalCredit;
            ws.Cell(row, 5).Value = line.Balance;
            row++;
        }

        ws.Cell(row, 1).Value = "الإجمالي";
        ws.Cell(row, 3).Value = dto.TotalDebit;
        ws.Cell(row, 4).Value = dto.TotalCredit;

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] ExportIncomeStatement(IncomeStatementDto dto)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("قائمة الدخل");
        ws.RightToLeft = true;

        ws.Cell(1, 1).Value = "قائمة الدخل";
        ws.Cell(2, 1).Value = $"من {dto.From:yyyy-MM-dd} إلى {dto.To:yyyy-MM-dd}";

        var row = 4;
        ws.Cell(row, 1).Value = "الإيرادات";
        row++;
        foreach (var r in dto.Revenues)
        {
            ws.Cell(row, 1).Value = r.AccountNameAr;
            ws.Cell(row, 2).Value = r.Amount;
            row++;
        }
        ws.Cell(row, 1).Value = "إجمالي الإيرادات";
        ws.Cell(row, 2).Value = dto.TotalRevenue;
        row += 2;

        ws.Cell(row, 1).Value = "المصروفات";
        row++;
        foreach (var e in dto.Expenses)
        {
            ws.Cell(row, 1).Value = e.AccountNameAr;
            ws.Cell(row, 2).Value = e.Amount;
            row++;
        }
        ws.Cell(row, 1).Value = "إجمالي المصروفات";
        ws.Cell(row, 2).Value = dto.TotalExpenses;
        row += 2;

        ws.Cell(row, 1).Value = "صافي الدخل";
        ws.Cell(row, 2).Value = dto.NetIncome;

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] ExportBalanceSheet(BalanceSheetDto dto)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("الميزانية العمومية");
        ws.RightToLeft = true;

        ws.Cell(1, 1).Value = "الميزانية العمومية";
        ws.Cell(2, 1).Value = $"كما في {dto.AsOf:yyyy-MM-dd}";

        var row = 4;
        WriteSection(ws, ref row, "الأصول", dto.Assets, dto.TotalAssets);
        WriteSection(ws, ref row, "الخصوم", dto.Liabilities, dto.TotalLiabilities);
        WriteSection(ws, ref row, "حقوق الملكية", dto.Equity, dto.TotalEquity);

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSection(
        IXLWorksheet ws, ref int row, string title,
        List<StatementLineDto> lines, decimal total)
    {
        ws.Cell(row, 1).Value = title;
        row++;
        foreach (var line in lines)
        {
            ws.Cell(row, 1).Value = line.AccountNameAr;
            ws.Cell(row, 2).Value = line.Amount;
            row++;
        }
        ws.Cell(row, 1).Value = $"إجمالي {title}";
        ws.Cell(row, 2).Value = total;
        row += 2;
    }
}