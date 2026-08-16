using ERPSystem.Application.DTOs.Reports;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERPSystem.Web.Services;

/// <summary>
/// يولّد ملفات PDF من بيانات التقارير المالية باستخدام QuestPDF.
/// يسجّل خطاً عربياً من نظام ويندوز لعرض النصوص العربية بشكل صحيح.
/// </summary>
public static class PdfExporter
{
    public const string PdfContentType = "application/pdf";

    private const string ArabicFont = "Arabic";

    static PdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        TryRegisterArabicFont();
    }

    private static void TryRegisterArabicFont()
    {
        // نسجّل خط Arial (يدعم العربية) من مجلد خطوط ويندوز تحت اسم مخصص "Arabic".
        var candidates = new[]
        {
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\segoeui.ttf"
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                using var stream = File.OpenRead(path);
                FontManager.RegisterFontWithCustomName(ArabicFont, stream);
                return;
            }
            catch
            {
                // نتجاهل ونجرب الخط التالي
            }
        }
    }

    public static byte[] ExportTrialBalance(TrialBalanceDto dto)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily(ArabicFont).FontSize(9));

                page.Content().Column(column =>
                {
                    column.Item().Text("ميزان المراجعة").SemiBold().FontSize(16);
                    column.Item().Height(10);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);
                            columns.RelativeColumn();
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("الكود").SemiBold();
                            header.Cell().Text("الحساب").SemiBold();
                            header.Cell().Text("مدين").SemiBold();
                            header.Cell().Text("دائن").SemiBold();
                            header.Cell().Text("الرصيد").SemiBold();
                        });

                        foreach (var line in dto.Lines)
                        {
                            table.Cell().Text(line.AccountCode);
                            table.Cell().Text(line.AccountNameAr);
                            table.Cell().Text(line.TotalDebit.ToString("N2"));
                            table.Cell().Text(line.TotalCredit.ToString("N2"));
                            table.Cell().Text(line.Balance.ToString("N2"));
                        }

                        table.Cell().Text("الإجمالي").SemiBold();
                        table.Cell().Text("");
                        table.Cell().Text(dto.TotalDebit.ToString("N2")).SemiBold();
                        table.Cell().Text(dto.TotalCredit.ToString("N2")).SemiBold();
                        table.Cell().Text("");
                    });
                });
            });
        }).GeneratePdf();
    }

    public static byte[] ExportIncomeStatement(IncomeStatementDto dto)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily(ArabicFont).FontSize(10));

                page.Content().Column(column =>
                {
                    column.Item().Text("قائمة الدخل").SemiBold().FontSize(16);
                    column.Item().Text($"من {dto.From:yyyy-MM-dd} إلى {dto.To:yyyy-MM-dd}").FontSize(10);
                    column.Item().Height(10);

                    WriteSection(column, "الإيرادات", dto.Revenues, dto.TotalRevenue);
                    column.Item().Height(10);
                    WriteSection(column, "المصروفات", dto.Expenses, dto.TotalExpenses);
                    column.Item().Height(10);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("صافي الدخل").SemiBold();
                        row.ConstantItem(100).Text(dto.NetIncome.ToString("N2")).SemiBold();
                    });
                });
            });
        }).GeneratePdf();
    }

    public static byte[] ExportBalanceSheet(BalanceSheetDto dto)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily(ArabicFont).FontSize(10));

                page.Content().Column(column =>
                {
                    column.Item().Text("الميزانية العمومية").SemiBold().FontSize(16);
                    column.Item().Text($"كما في {dto.AsOf:yyyy-MM-dd}").FontSize(10);
                    column.Item().Height(10);

                    WriteSection(column, "الأصول", dto.Assets, dto.TotalAssets);
                    column.Item().Height(10);
                    WriteSection(column, "الخصوم", dto.Liabilities, dto.TotalLiabilities);
                    column.Item().Height(10);
                    WriteSection(column, "حقوق الملكية", dto.Equity, dto.TotalEquity);
                });
            });
        }).GeneratePdf();
    }

    private static void WriteSection(
        ColumnDescriptor column, string title,
        List<StatementLineDto> lines, decimal total)
    {
        column.Item().Text(title).SemiBold().FontSize(12);
        column.Item().Height(4);

        foreach (var line in lines)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(line.AccountNameAr);
                row.ConstantItem(120).Text(line.Amount.ToString("N2"));
            });
        }

        column.Item().Row(row =>
        {
            row.RelativeItem().Text($"إجمالي {title}").SemiBold();
            row.ConstantItem(120).Text(total.ToString("N2")).SemiBold();
        });
    }
}