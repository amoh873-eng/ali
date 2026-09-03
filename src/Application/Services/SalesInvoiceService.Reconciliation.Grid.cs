using System.Globalization;

namespace ERPSystem.Application.Services;

/// <summary>
/// SalesInvoiceService — محلل الشبكة العام لصفوف كشف البنك:
/// يكتشف أعمدة الترويسة (عربي/إنجليزي) ويوزع المبالغ والتواريخ بأشكال شائعة.
/// </summary>
public partial class SalesInvoiceService
{
    // ────────────────────────── مُحلّل شبكة مشترك ──────────────────────────

    private static List<(string Reference, decimal Amount, DateTime Date)> ParseGrid(List<List<string>> grid)
    {
        var rows = new List<(string, decimal, DateTime)>();
        if (grid.Count == 0) return rows;

        var header = grid[0].Select(h => (h ?? string.Empty).Trim().ToLowerInvariant()).ToList();
        var refIdx = FindColumn(header, new[]
        {
            "مرجع", "المرجع", "رقم العملية", "رقم الموافقة", "رقم الاذن",
            "ref", "reference", "approval", "approval code", "auth", "authorization",
            "tranid", "transaction id", "رقم المرجع"
        });
        var amtIdx = FindColumn(header, new[]
        {
            "المبلغ", "القيمة", "اجمالى", "إجمالي", "المبلغ بالدينار",
            "amount", "value", "total", "amt", "net amount", "amount (jod)"
        });
        var dateIdx = FindColumn(header, new[]
        {
            "التاريخ", "تاريخ", "date", "transaction date", "توقيت", "time", "purchase date"
        });

        bool hasHeader = header.Any(h =>
            h.Contains("reference") || h.Contains("مرجع") || h.Contains("رقم") ||
            h.Contains("amount") || h.Contains("المبلغ") || h.Contains("قيمة") ||
            h.Contains("date") || h.Contains("التاريخ"));

        var startRow = hasHeader ? 1 : 0;

        for (var i = startRow; i < grid.Count; i++)
        {
            var row = grid[i];
            if (row.All(string.IsNullOrWhiteSpace)) continue;

            string reference;
            decimal amount;
            DateTime date;

            if (refIdx >= 0 && refIdx < row.Count)
                reference = row[refIdx];
            else if (row.Count >= 1) reference = row[0];
            else continue;

            if (string.IsNullOrWhiteSpace(reference)) continue;

            var amountCell = amtIdx >= 0 && amtIdx < row.Count ? row[amtIdx] : (row.Count >= 2 ? row[^1] : "");
            if (!TryParseAmount(amountCell, out amount)) continue;

            var dateCell = dateIdx >= 0 && dateIdx < row.Count ? row[dateIdx] : (row.Count >= 3 ? row[1] : "");
            if (!TryParseDate(dateCell, out date)) date = DateTime.Today;

            rows.Add((reference.Trim(), amount, date));
        }

        return rows;
    }

    private static int FindColumn(List<string> header, string[] candidates)
    {
        for (var i = 0; i < header.Count; i++)
        {
            var h = header[i];
            if (string.IsNullOrWhiteSpace(h)) continue;
            if (candidates.Any(c => h.Contains(c, StringComparison.OrdinalIgnoreCase)))
                return i;
        }
        return -1;
    }

    private static bool TryParseAmount(string raw, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var s = raw.Trim()
            .Replace("ج.م", "").Replace("JOD", "").Replace("دينار", "").Replace("د.ا", "")
            .Replace("JD", "").Replace("$", "").Replace("€", "")
            .Replace(" ", "").Replace("\u00A0", "")
            .Replace("\u066C", "")   // فاصل الآلاف العربي (٬)
            .Replace("\u066B", "."); // الفاصلة العشرية العربية (٫)

        // تنسيق أوروبي (1.234,56) مقابل أمريكي (1,234.56): إن وُجدت العلاقتان معاً نعتمد الأخيرة.
        var hasBoth = s.Contains(',') && s.Contains('.');
        if (hasBoth)
        {
            if (s.LastIndexOf(',') > s.LastIndexOf('.')) // الأوروبي: الفاصلة تفصل الكسور
                s = s.Replace(".", "").Replace(',', '.');
        }
        else
        {
            // فاصلة واحدة فقط بعدها 3 خانات مع وجود نقطة = فاصل آلاف (تنسيق أمريكي)
            if (s.Count(c => c == ',') == 1 && s.IndexOf(',') > 0
                && s.Length - s.IndexOf(',') - 1 == 3 && s.Contains('.'))
                s = s.Replace(",", "");
        }

        return decimal.TryParse(s,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture, out amount);
    }

    private static bool TryParseDate(string raw, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var s = raw.Trim();

        // XLSX قد يخزن التاريخ كرقم تسلسلي (عدد الأيام منذ 1900-01-01 مع خطأ اللحظة)
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial)
            && serial >= 1 && serial < 60000)
        {
            var baseDate = new DateTime(1899, 12, 30);
            date = baseDate.AddDays(serial);
            if (date.Year > 2100 || (serial >= 60 && serial < 61)) date = baseDate.AddDays(serial - 1);
            return true;
        }

        var formats = new[]
        {
            "dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm",
            "MM/dd/yyyy", "MM/dd/yyyy HH:mm:ss",
            "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd",
            "dd-MM-yyyy", "dd.MM.yyyy", "dd/MM/yy"
        };
        return DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}