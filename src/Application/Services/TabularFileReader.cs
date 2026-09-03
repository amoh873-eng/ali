using System.Text;
using ClosedXML.Excel;
using ERPSystem.Application.DTOs.Items;

namespace ERPSystem.Application.Services;

/// <summary>
/// قارئ ملفات جدولية للاستيراد الجماعي:
/// - .xlsx عبر ClosedXML (المكتبة نفسها المستخدمة للتصدير — إعادة استخدام بلا تبعية جديدة)
/// - .csv / .txt مع كشف تلقائي للمحدد (فاصلة / فاصلة منقوطة / Tab) ودعم الأعمدة المقتبسة
/// - .xls القديم: تُكتشف بصمة OLE2 ويعود خطأ واضح ومحدد (إعادة الحفظ بـ .xlsx أو .csv)
///
/// أي صيغة لا يمكن التعامل معها بثقة تفشل مبكراً برسالة محددة — لا محاولة تجزئة جزئية.
/// </summary>
public static class TabularFileReader
{
    private const int MaxRows = 100_000;
    private const int MaxCols = 100;

    public static ParsedImportFileDto Read(Stream stream, string fileName)
    {
        if (stream is null) throw new InvalidOperationException("لم يتم اختيار ملف.");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        switch (ext)
        {
            case ".xlsx":
                return ReadXlsx(stream);
            case ".csv":
            case ".txt":
                return ReadDelimited(stream);
            case ".xls":
                throw new InvalidOperationException(
                    "صيغة Excel القديمة (.xls) غير مدعومة للقراءة المباشرة — احفظ الملف بصيغة .xlsx (حفظ باسم → Excel Workbook) أو .csv ثم أعد الرفع.");
            default:
                throw new InvalidOperationException("صيغة غير مدعومة — ارفع ملف .xlsx أو .csv أو .txt فقط.");
        }
    }

    private static ParsedImportFileDto ReadXlsx(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException("ملف Excel لا يحتوي على أي ورقة عمل.");

            var grid = new List<List<string>>();
            foreach (var row in ws.RowsUsed())
            {
                var lastCol = 0;
                foreach (var cell in row.CellsUsed())
                {
                    lastCol = Math.Max(lastCol, cell.Address.ColumnNumber);
                }
                lastCol = Math.Min(lastCol, MaxCols);

                var cells = new List<string>(lastCol);
                for (var c = 1; c <= lastCol; c++)
                {
                    var cell = row.Cell(c);
                    cells.Add(cell.IsEmpty() ? string.Empty : cell.GetFormattedString().Trim());
                }
                grid.Add(cells);

                if (grid.Count >= MaxRows) break;
            }

            return BuildResult(grid);
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "تعذّرت قراءة ملف Excel — تأكد أن الملف سليم وغير محمي بكلمة مرور (" + ex.Message + ").");
        }
    }

    // ────────────────────────── CSV / TXT ──────────────────────────

    private static ParsedImportFileDto ReadDelimited(Stream stream)
    {
        string firstLine;
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false))
        {
            var lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
                if (lines.Count >= MaxRows) break;
            }
            if (lines.Count == 0)
                throw new InvalidOperationException("الملف فارغ — لا يحتوي على أي صفوف.");
            firstLine = lines[0];

            var delimiter = DetectDelimiter(firstLine);
            var grid = new List<List<string>>();
            foreach (var l in lines)
            {
                grid.Add(SplitCsvLine(l, delimiter));
                if (grid.Count >= MaxRows) break;
            }
            return BuildResult(grid);
        }
    }

    /// <summary>يكتشف المحدد من أول سطر (الأكثر تكراراً خارج الاقتباس: Tab ثم ; ثم ,).</summary>
    private static char DetectDelimiter(string firstLine)
    {
        var tabs = CountOutsideQuotes(firstLine, '\t');
        var semis = CountOutsideQuotes(firstLine, ';');
        var commas = CountOutsideQuotes(firstLine, ',');

        if (semis > commas && semis >= tabs) return ';';
        if (commas > 0 && commas >= semis && commas >= tabs) return ',';
        if (tabs > 0) return '\t';

        throw new InvalidOperationException(
            "تعذّر اكتشاف المحدد بين الأعمدة — تأكد أن الملف جدولي مفصول بفواصل أو فاصلة منقوطة أو Tab وأنه يحتوي على عمودين على الأقل.");
    }

    private static int CountOutsideQuotes(string line, char target)
    {
        var count = 0;
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { i++; continue; }
                inQuotes = !inQuotes;
            }
            else if (ch == target && !inQuotes)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>تقسيم سطر CSV مع احترام الاقتباس المزدوج ("...") وفك تكراره داخل النص.</summary>
    private static List<string> SplitCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        void Flush()
        {
            result.Add(current.ToString().Trim().Trim('"'));
            current.Clear();
        }

        while (i < line.Length)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i += 2;
                    continue;
                }
                inQuotes = !inQuotes;
                i++;
                continue;
            }
            if (ch == delimiter && !inQuotes)
            {
                Flush();
                i++;
                continue;
            }
            current.Append(ch);
            i++;
        }
        Flush();
        return result;
    }

    // ────────────────────────── بناء الناتج المشترك ──────────────────────────

    private static ParsedImportFileDto BuildResult(List<List<string>> grid)
    {
        while (grid.Count > 0 && grid[0].All(string.IsNullOrWhiteSpace))
            grid.RemoveAt(0);

        if (grid.Count == 0)
            throw new InvalidOperationException("الملف فارغ — لا يحتوي على صف ترويسة أو بيانات.");

        var headers = new List<string>(grid[0]);
        var rows = new List<List<string>>(grid.Skip(1));

        if (headers.Count < 2)
            throw new InvalidOperationException(
                "يبدو أن الملف يحتوي على عمود واحد فقط — تأكد أن الصف الأول يمثل الترويسة وأنه يوجد عمودا اسم وباركود على الأقل.");

        // إزالة أسماء أعمدة مكررة (Excel يسمح بها) بتلقيح اللاحقة الرقمية
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            var h = headers[i];
            if (string.IsNullOrWhiteSpace(h)) h = $"عمود {i + 1}";
            var candidate = h;
            var n = 2;
            while (!seen.Add(candidate)) candidate = $"{h} ({n++})";
            headers[i] = candidate;
        }

        return new ParsedImportFileDto
        {
            Headers = headers,
            Rows = rows,
            SuggestedMap = BuildSuggestedMap(headers)
        };
    }

    /// <summary>
    /// يقترح تعيين الأعمدة من أسماء ترويسات شائعة (عربي/إنجليزي).
    /// الحقان الإلزاميان مضمونان دائماً: عمود 0 للاسم، وعمود 1 أو 0 للباركود.
    /// </summary>
    internal static BulkImportColumnMapDto BuildSuggestedMap(List<string> headers)
    {
        var nameKey = new[] { "اسم", "name", "الاسم", "item", "product" };
        var barcodeKey = new[] { "باركود", "barcode", "رمز", "كود", "sku" };
        var costKey = new[] { "تكلفة", "شراء", "cost", "purchase", "buy", "التكلفة" };
        var saleKey = new[] { "بيع", "سعر", "price", "sale", "البيع" };
        var catKey = new[] { "فئة", "تصنيف", "category", "الفئة" };
        var unitKey = new[] { "وحدة", "unit", "الوحدة" };

        var map = new BulkImportColumnMapDto { NameColumn = -1, BarcodeColumn = -1 };
        var taken = new bool[headers.Count];

        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim();
            if (string.IsNullOrWhiteSpace(header)) continue;

            if (map.NameColumn < 0 && Matches(header, nameKey)) { map.NameColumn = i; taken[i] = true; continue; }
            if (map.BarcodeColumn < 0 && Matches(header, barcodeKey)) { map.BarcodeColumn = i; taken[i] = true; continue; }
            if (!map.CostColumn.HasValue && Matches(header, costKey)) { map.CostColumn = i; taken[i] = true; continue; }
            if (!map.SaleColumn.HasValue && Matches(header, saleKey)) { map.SaleColumn = i; taken[i] = true; continue; }
            if (!map.CategoryColumn.HasValue && Matches(header, catKey)) { map.CategoryColumn = i; taken[i] = true; continue; }
            if (!map.UnitColumn.HasValue && Matches(header, unitKey)) { map.UnitColumn = i; taken[i] = true; continue; }
        }

        // ضمان العمودين الإلزاميين (fallback صناعي)
        if (map.NameColumn < 0)
        {
            map.NameColumn = 0;
            taken[0] = true;
        }
        if (map.BarcodeColumn < 0)
        {
            map.BarcodeColumn = map.NameColumn == 0 && headers.Count > 1 ? 1 : 0;
            taken[map.BarcodeColumn] = true;
        }

        return map;
    }

    private static bool Matches(string header, string[] keys)
        => keys.Any(k => header.Contains(k, StringComparison.OrdinalIgnoreCase));
}