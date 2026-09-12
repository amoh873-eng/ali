using System.Diagnostics;
using System.Globalization;
using ClosedXML.Excel;
using ERPSystem.Application.DTOs.Items;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// ItemService — الاستيراد الجماعي للأصناف (Bulk Import).
/// الاسم+الباركود إلزاميان؛ الأسعار اختيارية (فارغ=0)؛ الفئة/الوحدة ببديل افتراضي.
/// الباركود الموجود مسبقاً ← يُتخطى (القرار المعتمد). الأداء: فحص مسبق مرة واحدة،
/// توليد أكواد بدفعات، حفظ بدفعات 500 — بلا N+1 ولا SaveChanges لكل صف.
/// </summary>
public partial class ItemService
{
    private const int ImportBatchSize = 500;
    private const string ItemCodePrefix = "ITM";

    /// <summary>
    /// علامة زمنية (على مستوى التطبيق قيد التشغيل) لآخر دفعة استيراد جماعي اكتملت.
    /// تُستخدم في شاشة ملصقات الباركود لاختيار "كل الأصناف من آخر دفعة استيراد"
    /// دون الحاجة لتغيير schema (ميزة فعلية ومفيدة بعد استيراد 500–5000 صنف دفعة واحدة).
    /// </summary>
    private static DateTime? LastBulkImportCompletedAtUtc;

    public static DateTime? GetLastBulkImportTimeUtc() => LastBulkImportCompletedAtUtc;

    public async Task<ParsedImportFileDto> ParseImportFileAsync(Stream fileStream, string fileName)
    {
        // دفق المتصفح يمنع القراءة المتزامنة — انسخ غير متزامن ثم حلّل من الذاكرة
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer);
        buffer.Position = 0;
        return await Task.Run(() => TabularFileReader.Read(buffer, fileName));
    }

    public async Task<ItemImportPreviewDto> BuildImportPreviewAsync(ParsedImportFileDto file, BulkImportColumnMapDto map)
    {
        var existingBarcodes = await ExistingBarcodeSetAsync();
        var categories = await _context.Set<Category>().Where(c => !c.IsDeleted).ToListAsync();
        var units = await _context.Set<Unit>().Where(u => !u.IsDeleted).ToListAsync();

        var preview = new ItemImportPreviewDto { TotalRows = file.TotalRows };
        foreach (var row in InterpretAllRows(file, map, existingBarcodes, categories, units))
        {
            switch (row.Status)
            {
                case ItemImportRowStatus.Ready: preview.ValidCount++; break;
                case ItemImportRowStatus.ExistsInDatabase: preview.ExistsInDbCount++; break;
                case ItemImportRowStatus.DuplicateInFile: preview.DuplicateInFileCount++; break;
                default: preview.ErrorCount++; break;
            }
            preview.NoticeCount += row.Notices.Count;
            if (preview.SampleRows.Count < 100) preview.SampleRows.Add(row);
        }
        return preview;
    }

    /// <summary>مجموعة الباركودات الموجودة (استعلام واحد مسبق — يمنع مشكلة N+1 أثناء 5000+ صف).</summary>
    private async Task<HashSet<string>> ExistingBarcodeSetAsync()
    {
        var barcodes = await _context.Set<Item>()
            .Where(i => !i.IsDeleted && i.Barcode != null)
            .Select(i => i.Barcode!)
            .ToListAsync();
        return new HashSet<string>(barcodes.Select(Norm), StringComparer.Ordinal);
    }

    private static string Norm(string s) => (s ?? string.Empty).Trim().ToLowerInvariant();

    private static Category? MatchCategory(string input, List<Category> categories)
    {
        var n = Norm(input);
        return categories.FirstOrDefault(c =>
            Norm(c.NameAr) == n || Norm(c.Code) == n || Norm(c.NameEn ?? string.Empty) == n);
    }

    private static Unit? MatchUnit(string input, List<Unit> units)
    {
        var n = Norm(input);
        return units.FirstOrDefault(u =>
            Norm(u.NameAr) == n || Norm(u.Code) == n || Norm(u.NameEn ?? string.Empty) == n);
    }

    /// <summary>قبول أشكال الأرقام الشائعة (عربي/أوروبي/أمريكي) مع تحويل الحروف.</summary>
    internal static bool TryParseDecimal(string raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var s = raw.Trim()
            .Replace("\u066C", "")      // فاصل الآلاف العربي ٬
            .Replace("\u066B", ".")     // فاصلة عشرية عربية ٫
            .Replace(" ", "")
            .Replace("\u00A0", "");

        var hasBoth = s.Contains(',') && s.Contains('.');
        if (hasBoth)
        {
            if (s.LastIndexOf(',') > s.LastIndexOf('.')) // أوروبي: الفاصلة قبل الكسور
                s = s.Replace(".", "").Replace(',', '.');
        }
        else if (s.Count(c => c == ',') == 1 && s.IndexOf(',') > 0
                 && s.Length - s.IndexOf(',') - 1 == 3 && s.Contains('.'))
        {
            s = s.Replace(",", ""); // أمريكي: فاصل آلاف بثلاث خانات
        }

        return decimal.TryParse(s,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture, out value);
    }

    /// <summary>يفسّر كل صفوف الملف ويفحصها (يستخدمه المعاينة والتنفيذ).</summary>
    private static List<ItemImportPreviewRowDto> InterpretAllRows(
        ParsedImportFileDto file,
        BulkImportColumnMapDto map,
        HashSet<string> existingBarcodes,
        List<Category> categories,
        List<Unit> units)
    {
        if (!map.IsComplete)
            throw new InvalidOperationException("يجب تعيين عمودي اسم الصنف والباركود أولاً.");

        var rows = new List<ItemImportPreviewRowDto>(file.TotalRows);
        var seenBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // رقم الصف في الملف الأصلي: الترويسة هي الصف 1، فالصف الأول للبيانات هو 2
        var rowNumber = 2;
        foreach (var raw in file.Rows)
        {
            rows.Add(InterpretRow(rowNumber++, raw, map, existingBarcodes, categories, units, seenBarcodes));
        }
        return rows;
    }

    private static ItemImportPreviewRowDto InterpretRow(
        int rowNumber,
        IReadOnlyList<string> raw,
        BulkImportColumnMapDto map,
        HashSet<string> existingBarcodes,
        List<Category> categories,
        List<Unit> units,
        HashSet<string> seenBarcodes)
    {
        string Cell(int idx) => idx >= 0 && idx < raw.Count ? raw[idx].Trim() : string.Empty;

        var row = new ItemImportPreviewRowDto
        {
            RowNumber = rowNumber,
            Name = Cell(map.NameColumn),
            Barcode = Cell(map.BarcodeColumn),
            CategoryInput = map.CategoryColumn.HasValue ? Cell(map.CategoryColumn.Value) : string.Empty,
            UnitInput = map.UnitColumn.HasValue ? Cell(map.UnitColumn.Value) : string.Empty
        };

        var nameMissing = string.IsNullOrWhiteSpace(row.Name);
        var barcodeMissing = string.IsNullOrWhiteSpace(row.Barcode);
        var barcodeKey = barcodeMissing ? string.Empty : Norm(row.Barcode);
        var blocking = 0;
        var costBad = false;
        var saleBad = false;
        var dupInFile = false;

        // ── الحقلان الإلزاميان ──
        if (nameMissing) { row.Issues.Add("اسم الصنف مفقود — الصف مرفوض."); blocking++; }
        if (barcodeMissing) { row.Issues.Add("الباركود مفقود — الصف مرفوض."); blocking++; }

        // ── الأسعار الاختيارية ──
        var costRaw = map.CostColumn.HasValue ? Cell(map.CostColumn.Value) : string.Empty;
        var saleRaw = map.SaleColumn.HasValue ? Cell(map.SaleColumn.Value) : string.Empty;

        if (!string.IsNullOrWhiteSpace(costRaw))
        {
            if (TryParseDecimal(costRaw, out var cost) && cost >= 0) row.CostPrice = cost;
            else { costBad = true; row.Issues.Add("قيمة سعر الشراء غير صالحة (رقم سالب أو نص)."); blocking++; }
        }
        if (!string.IsNullOrWhiteSpace(saleRaw))
        {
            if (TryParseDecimal(saleRaw, out var sale) && sale >= 0) row.SalePrice = sale;
            else { saleBad = true; row.Issues.Add("قيمة سعر البيع غير صالحة (رقم سالب أو نص)."); blocking++; }
        }

        // ── التكرار داخل الملف / الوجود في النظام ──
        var existsInDb = false;
        if (!barcodeMissing)
        {
            if (!seenBarcodes.Add(barcodeKey))
            {
                dupInFile = true;
                row.Issues.Add("باركود مكرر داخل الملف — يُرفض الصف الثاني.");
                blocking++;
            }
            else if (existingBarcodes.Contains(barcodeKey))
            {
                existsInDb = true;
            }
        }

        // ── الفئة والوحدة (ببديل افتراضي وملاحظة) ──
        if (string.IsNullOrWhiteSpace(row.CategoryInput))
        {
            row.CategoryMatchedTo = "غير مصنف";
            row.Notices.Add("الفئة غير محددة — ستُستخدم «غير مصنف».");
        }
        else
        {
            var cat = MatchCategory(row.CategoryInput, categories);
            if (cat is null)
            {
                row.CategoryMatchedTo = "غير مصنف";
                row.Notices.Add($"الفئة «{row.CategoryInput}» غير معروفة — ستُستخدم «غير مصنف».");
            }
            else row.CategoryMatchedTo = cat.NameAr;
        }

        if (string.IsNullOrWhiteSpace(row.UnitInput))
        {
            row.UnitMatchedTo = "قطعة";
            row.Notices.Add("الوحدة غير محددة — ستُستخدم «قطعة».");
        }
        else
        {
            var unit = MatchUnit(row.UnitInput, units);
            if (unit is null)
            {
                row.UnitMatchedTo = "قطعة";
                row.Notices.Add($"الوحدة «{row.UnitInput}» غير معروفة — ستُستخدم «قطعة».");
            }
            else row.UnitMatchedTo = unit.NameAr;
        }

        // ── الحالة النهائية ──
        if (blocking > 0)
        {
            if (nameMissing) row.Status = ItemImportRowStatus.MissingName;
            else if (barcodeMissing) row.Status = ItemImportRowStatus.MissingBarcode;
            else if (dupInFile) row.Status = ItemImportRowStatus.DuplicateInFile;
            else if (costBad && saleBad) row.Status = ItemImportRowStatus.InvalidCostAndSale;
            else if (costBad) row.Status = ItemImportRowStatus.InvalidCost;
            else if (saleBad) row.Status = ItemImportRowStatus.InvalidSale;
        }
        else if (existsInDb)
        {
            row.Status = ItemImportRowStatus.ExistsInDatabase;
            row.Issues.Add("الباركود موجود مسبقاً في النظام — سيُتخطى الصف بلا تعديل.");
        }
        else
        {
            row.Status = ItemImportRowStatus.Ready;
        }

        return row;
    }

    // ────────────────────────── التنفيذ المُدفّع ──────────────────────────

    public async Task<ItemImportResultDto> ExecuteBulkImportAsync(
        ParsedImportFileDto file,
        BulkImportColumnMapDto map,
        Action<int, int>? progress = null)
    {
        var sw = Stopwatch.StartNew();
        if (!map.IsComplete)
            throw new InvalidOperationException("يجب تعيين عمودي اسم الصنف والباركود أولاً.");

        var result = new ItemImportResultDto { TotalRows = file.TotalRows };

        // إعادة البناء داخلياً — لا نثق ببيانات عميل المعاينة
        var existingBarcodes = await ExistingBarcodeSetAsync();
        var categories = await _context.Set<Category>().Where(c => !c.IsDeleted).ToListAsync();
        var units = await _context.Set<Unit>().Where(u => !u.IsDeleted).ToListAsync();
        var allRows = InterpretAllRows(file, map, existingBarcodes, categories, units);

        var defaultCategory = await EnsureDefaultCategoryAsync(categories);
        var defaultUnit = await EnsureDefaultUnitAsync(units);

        var usedCodes = new HashSet<string>(
            await _context.Set<Item>().Where(i => !i.IsDeleted).Select(i => i.Code).ToListAsync(),
            StringComparer.Ordinal);

        var pending = new List<Item>();
        var processed = 0;

        foreach (var row in allRows)
        {
            processed++;
            if (processed % 100 == 0 || processed == allRows.Count)
                progress?.Invoke(processed, allRows.Count);

            switch (row.Status)
            {
                case ItemImportRowStatus.Ready:
                {
                    var categoryId = defaultCategory.Id;
                    var matchedCat = MatchCategory(row.CategoryInput, categories);
                    if (matchedCat is not null) categoryId = matchedCat.Id;

                    var unitId = defaultUnit.Id;
                    var matchedUnit = MatchUnit(row.UnitInput, units);
                    if (matchedUnit is not null) unitId = matchedUnit.Id;

                    pending.Add(new Item
                    {
                        Id = Guid.NewGuid(),
                        NameAr = row.Name,
                        NameEn = null,
                        CategoryId = categoryId,
                        UnitId = unitId,
                        CostPrice = row.CostPrice ?? 0m,
                        SalePrice = row.SalePrice ?? 0m,
                        MinStockLevel = 0,
                        MaxStockLevel = 0,
                        Barcode = row.Barcode,
                        Description = null,
                        IsActive = true,
                        IsSystem = false,
                        CurrentStock = 0,
                        TracksExpiry = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    break;
                }
                case ItemImportRowStatus.ExistsInDatabase:
                    result.SkippedExistingCount++;
                    result.Failures.Add(new ItemImportFailureDto
                    {
                        RowNumber = row.RowNumber, Name = row.Name, Barcode = row.Barcode,
                        Reason = "الباركود موجود مسبقاً في النظام — تخطٍّ (بلا تعديل)."
                    });
                    break;
                default:
                    result.FailedCount++;
                    result.Failures.Add(new ItemImportFailureDto
                    {
                        RowNumber = row.RowNumber, Name = row.Name, Barcode = row.Barcode,
                        Reason = string.Join("؛ ", row.Issues)
                    });
                    break;
            }
        }

        // حفظ بدفعات مع توليد الأكواد لكل دفعة استدعاءً واحداً (Atomic)
        for (var i = 0; i < pending.Count; i += ImportBatchSize)
        {
            var batch = pending.Skip(i).Take(ImportBatchSize).ToList();

            var k = 0;
            while (k < batch.Count)
            {
                var codes = await NumberSequenceHelper.NextBatchAsync(_context, ItemCodePrefix, batch.Count - k);
                foreach (var code in codes)
                {
                    if (usedCodes.Contains(code)) continue; // تصادم نادر — نتابع للتالي
                    batch[k].Code = code;
                    usedCodes.Add(code);
                    k++;
                    if (k >= batch.Count) break;
                }
            }

            _context.Set<Item>().AddRange(batch);
            await _context.SaveChangesAsync();
            result.CreatedCount += batch.Count;

            progress?.Invoke(allRows.Count + Math.Min(i + batch.Count, pending.Count), allRows.Count + pending.Count);
        }

        result.NoticeCount = allRows.Sum(r => r.Notices.Count);
        result.ElapsedMs = sw.ElapsedMilliseconds;

        // تسجيل لحظة اكتمال الدفعة (تُستخدم في شاشة ملصقات الباركود → "آخر دفعة استيراد")
        if (result.CreatedCount > 0)
            LastBulkImportCompletedAtUtc = DateTime.UtcNow;

        return result;
    }

    /// <summary>
    /// يُعيد الأصناف المنشأة ضمن "آخر دفعة استيراد جماعي" (التي اكتملت في هذا التشغيل).
    /// إن لم توجد دفعة ← قائمة فارغة.
    /// </summary>
    public async Task<List<ItemDto>> GetItemsFromLastBulkImportAsync()
    {
        var marker = LastBulkImportCompletedAtUtc;
        if (marker is null) return new List<ItemDto>();

        var items = await _context.Set<Item>()
            .Include(i => i.Category)
            .Include(i => i.Unit)
            .Where(i => !i.IsDeleted && i.CreatedAt >= marker)
            .OrderBy(i => i.Code)
            .ToListAsync();

        return items.Select(MapToDto).ToList();
    }

    /// <summary>فئة «غير مصنف»: تُوجد أو تُنشأ مرة واحدة (ملاحظة فقط، لا خطأ).</summary>
    private async Task<Category> EnsureDefaultCategoryAsync(List<Category> cache)
    {
        var existing = cache.FirstOrDefault(c => Norm(c.Code) == "uncat" || Norm(c.NameAr) == "غير مصنف");
        if (existing is not null) return existing;

        var created = new Category
        {
            Id = Guid.NewGuid(),
            Code = "UNCAT",
            NameAr = "غير مصنف",
            NameEn = "Uncategorized",
            IsActive = true,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Set<Category>().Add(created);
        await _context.SaveChangesAsync();
        cache.Add(created);
        return created;
    }

    /// <summary>وحدة «قطعة» (PCS): موجودة في البذور؛ نُنشئها إن حُذفت.</summary>
    private async Task<Unit> EnsureDefaultUnitAsync(List<Unit> cache)
    {
        var existing = cache.FirstOrDefault(u => Norm(u.Code) == "pcs" || Norm(u.NameAr) == "قطعة");
        if (existing is not null) return existing;

        var created = new Unit
        {
            Id = Guid.NewGuid(),
            Code = "PCS",
            NameAr = "قطعة",
            NameEn = "Piece",
            IsActive = true,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Set<Unit>().Add(created);
        await _context.SaveChangesAsync();
        cache.Add(created);
        return created;
    }

    /// <summary>يبني ملف Excel (xlsx) بأخطاء/تخطّي الاستيراد لتنزيله وإصلاحه وإعادة رفعه.</summary>
    public byte[] BuildImportFailuresReport(List<ItemImportFailureDto> failures)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("أخطاء الاستيراد");
        ws.RightToLeft = true;

        ws.Cell(1, 1).Value = "رقم الصف";
        ws.Cell(1, 2).Value = "اسم الصنف";
        ws.Cell(1, 3).Value = "الباركود";
        ws.Cell(1, 4).Value = "سبب الرفض";

        var row = 2;
        foreach (var f in failures)
        {
            ws.Cell(row, 1).Value = f.RowNumber;
            ws.Cell(row, 2).Value = f.Name;
            ws.Cell(row, 3).Value = f.Barcode;
            ws.Cell(row, 4).Value = f.Reason;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}