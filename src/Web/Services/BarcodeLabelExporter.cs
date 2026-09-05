using ERPSystem.Application.DTOs.Items;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZXing;
using ZXing.Common;

namespace ERPSystem.Web.Services;

/// <summary>
/// يولّد ملف PDF بملصقات باركود الأصناف (A4 — شبكة 3×8 = 24 ملصقاً بالورقة، مقاس Avery L7165).
/// - النمط: Code128 (يقبل أي محتوى — باركودات الأصناف قد تكون دخولاً يدوياً بلا تحقق EAN-13).
/// - التوليد عبر QuestPDF بأشرطة Fluent من مصفوفة ZXing مباشرة (بلا وسيط صور — طباعة سليمة 100%).
/// المحتوى: باركود (أكبر مساحة) + اسم مقطوع "…" (لا يكسر التخطيط) + الكود + السعر (اختياري).
/// </summary>
public static class BarcodeLabelExporter
{
    public const string PdfContentType = "application/pdf";

    // ── مقاس الشبكة (معلن للمراجعة والاعتماد قبل البناء) ──
    // A4: 297×210مم — خلايا 63.5×33.9مم، 3 أعمدة × 8 صفوف = 24 ملصقاً/صفحة
    private const int Cols = 3;
    private const int Rows = 8;

    private const float LabelWidthMm = 63.5f;
    private const float LabelHeightMm = 33.9f;
    private const float PageWidthPt = 595.28f;   // A4 عرض بالنقاط
    private const float PageHeightPt = 841.89f;  // A4 ارتفاع بالنقاط

    private const float MmToPt = 2.8346f;        // 1 mm ≈ 2.8346 pt

    private static readonly float LabelWidthPt = LabelWidthMm * MmToPt;   // ≈ 180
    private static readonly float LabelHeightPt = LabelHeightMm * MmToPt; // ≈ 96.1
    private static readonly float MarginXpt = (PageWidthPt - LabelWidthPt * Cols) / 2f;   // ≈ 27.6
    private static readonly float MarginYpt = (PageHeightPt - LabelHeightPt * Rows) / 2f; // ≈ 36.6
    private static readonly float CellGapPt = 0.5f;

    // أبعاد مناطق المحتوى داخل الملصق (نقاط)
    private const float BarcodeAreaHeightPt = 50f;
    private const float BarcodeMaxWidthPt = 164f;
    private const float Padded = 3f;

    static BarcodeLabelExporter()
    {
        // QuestPDF: الوضع المجتمعي (مشروع شركة صغيرة)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(List<BarcodeLabelItemDto> items, bool showPrice, int copiesPerItem = 1)
    {
        if (items is null || items.Count == 0) return Array.Empty<byte>();

        var copies = new List<BarcodeLabelItemDto>();
        var n = Math.Max(1, copiesPerItem);
        foreach (var it in items)
            for (var c = 0; c < n; c++) copies.Add(it);

        if (copies.Count == 0) return Array.Empty<byte>();

        var document = Document.Create(container =>
        {
            var idx = 0;
            while (idx < copies.Count)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(MarginYpt);
                    page.MarginBottom(MarginYpt);
                    page.MarginLeft(MarginXpt);
                    page.MarginRight(MarginXpt);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8));

                    page.Content().Column(col =>
                    {
                        for (var r = 0; r < Rows && idx < copies.Count; r++)
                        {
                            col.Item().Height(LabelHeightPt).Row(row =>
                            {
                                for (var c = 0; c < Cols; c++)
                                {
                                    if (idx < copies.Count)
                                    {
                                        var item = copies[idx++];
                                        row.RelativeItem(1).PaddingRight(CellGapPt).Element(cell => RenderLabel(cell, item, showPrice));
                                    }
                                    else
                                    {
                                        row.RelativeItem(1).PaddingRight(CellGapPt);
                                    }
                                }
                            });
                        }
                    });
                });
            }
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// يرسم محتوى ملصق واحد داخل صندوق الخلية (63.5×33.9مم):
    /// باركود (أكبر مساحة) ثم الاسم والكود والسعر اختيارياً.
    /// </summary>
    private static void RenderLabel(IContainer cell, BarcodeLabelItemDto item, bool showPrice)
    {
        cell.Padding(2f).Column(c =>
        {
            // الباركود (أكبر مساحة) — رسم بشرائح عبر Fluent (سليم 100% في الطباعة)
            c.Item().Height(BarcodeAreaHeightPt).AlignCenter().Width(BarcodeMaxWidthPt)
                .Element(bc => RenderBarcode(bc, item.Barcode ?? item.Code));

            // الاسم — مقطوع بلا كسر التخطيط
            c.Item().AlignCenter().Text(Truncate(item.NameAr, 42));

            // الكود
            c.Item().AlignCenter().Text(t => t.Span(item.Code).FontColor(Colors.Grey.Darken2).FontSize(7));

            // السعر (اختياري)
            if (showPrice)
            {
                c.Item().AlignCenter().Text(t =>
                {
                    t.Span("السعر: ").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                    t.Span(item.SalePrice.ToString("N2")).FontSize(8.5f).Bold();
                });
            }
        });
    }

    /// <summary>
    /// يرسم الباركود بأشرطة سوداء/بيضاء عبر عناصر Fluent (ConstantItem) —
    /// يقبل الطباعة تماماً ولا يعتمد على Canvas/صور خارجية.
    /// </summary>
    private static void RenderBarcode(IContainer c, string value)
    {
        var matrix = EncodeBarcode(value);
        if (matrix is null)
        {
            c.AlignCenter().Text("NO-BARCODE").FontColor(Colors.Grey.Medium);
            return;
        }

        var midY = matrix.Height / 2;
        var totalW = (float)matrix.Width;
        var scale = BarcodeMaxWidthPt / totalW;
        var barH = Math.Min(BarcodeAreaHeightPt - 2f, matrix.Height * scale);

        c.Height(barH).Row(row =>
        {
            var x = 0;
            while (x < matrix.Width)
            {
                var dark = matrix[x, midY];
                var start = x;
                while (x < matrix.Width && matrix[x, midY] == dark) x++;
                var wPt = (x - start) * scale;
                if (wPt <= 0) continue;
                if (dark)
                    row.ConstantItem(wPt).Background(Colors.Black);
                else
                    row.ConstantItem(wPt).Background(Colors.White);
            }
        });
    }

    /// <summary>تشفير Code128 عبر ZXing → مصفوفة BitMatrix (نرسم منها الشرائح مباشرة).</summary>
    private static BitMatrix? EncodeBarcode(string value)
    {
        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 260,
                    Height = 44,
                    Margin = 1,
                    PureBarcode = true
                }
            };
            var pd = writer.Write(value);
            if (pd is null || pd.Pixels is null || pd.Pixels.Length == 0) return null;

            var matrix = new BitMatrix(pd.Width, pd.Height);
            var stride = pd.Width * 4;
            for (var y = 0; y < pd.Height; y++)
            {
                for (var x = 0; x < pd.Width; x++)
                {
                    var i = y * stride + x * 4;
                    var b = (int)pd.Pixels[i];
                    var g = (int)pd.Pixels[i + 1];
                    var r = (int)pd.Pixels[i + 2];
                    var lum = (r * 299 + g * 587 + b * 114) / 1000;
                    if (lum < 128) matrix[x, y] = true;
                }
            }
            return matrix;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>قصّ طول الاسم عند الحاجة (لا يكسر المربع — نضيف "…").</summary>
    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length <= max) return value;
        return value.Substring(0, Math.Max(1, max - 1)) + "…";
    }
}