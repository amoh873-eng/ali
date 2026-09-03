using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace ERPSystem.Application.Services;

/// <summary>
/// SalesInvoiceService — محللات كشف البنك (CSV / XLSX).
/// XLSX يُقرأ كملف ZIP: sharedStrings.xml (النصوص المشتركة) + sheet1.xml (البيانات).
/// </summary>
public partial class SalesInvoiceService
{
    // ────────────────────────── CSV ──────────────────────────

    private static List<(string Reference, decimal Amount, DateTime Date)> ParseCsvRows(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            lines.Add(trimmed);
        }
        if (lines.Count == 0) return new List<(string, decimal, DateTime)>();

        var delimiter = DetectDelimiter(lines[0]);
        var grid = lines.Select(l => SplitCsvLine(l, delimiter)).ToList();
        return ParseGrid(grid);
    }

    private static char DetectDelimiter(string header)
    {
        if (header.Contains('\t')) return '\t';
        var commas = header.Count(c => c == ',');
        var semis = header.Count(c => c == ';');
        return semis > commas ? ';' : ',';
    }

    private static List<string> SplitCsvLine(string line, char delimiter)
        => line.Split(delimiter).Select(c => c.Trim().Trim('"')).ToList();

    // ────────────────────────── XLSX ──────────────────────────

    private static List<(string Reference, decimal Amount, DateTime Date)> ParseXlsxRows(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

        var sharedStrings = new List<string>();
        var sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedEntry is not null)
        {
            using var s = sharedEntry.Open();
            var doc = XDocument.Load(s);
            sharedStrings = doc.Descendants()
                .Where(e => e.Name.LocalName == "si")
                .Select(si => string.Concat(si.Descendants()
                    .Where(t => t.Name.LocalName == "t")
                    .Select(t => t.Value)))
                .ToList();
        }

        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidOperationException("الملف لا يحتوي على ورقة عمل sheet1.xml — تأكد أنه ملف Excel صالح.");

        List<List<string>> grid;
        using (var s = sheetEntry.Open())
        {
            var doc = XDocument.Load(s);
            grid = doc.Descendants()
                .Where(e => e.Name.LocalName == "row")
                .Select(row => row.Elements()
                    .Where(c => c.Name.LocalName == "c")
                    .Select(c =>
                    {
                        var type = (string?)c.Attribute("t") ?? "n";
                        var value = (string?)c.Element(XName.Get("v", c.Name.NamespaceName)) ?? "";
                        return type switch
                        {
                            "s" when int.TryParse(value, out var idx) && idx >= 0 && idx < sharedStrings.Count
                                => sharedStrings[idx],
                            "inlineStr" => string.Concat(c.Descendants()
                                .Where(t => t.Name.LocalName == "t").Select(t => t.Value)),
                            _ => value
                        };
                    })
                    .ToList())
                .Where(r => r.Count > 0)
                .ToList();
        }

        return ParseGrid(grid);
    }
}