using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace ERPSystem.Mobile;

/// <summary>
/// نظام تصميم موحّد: خلفية فاتحة، بطاقات بيضاء مستديرة، لوحة نيلي (Indigo)،
/// أزرار تعبئة، شرائط حالة ملونة، وحقول إدخال مقروءة.
/// </summary>
public static class Theme
{
    // ── اللوحة الأساسية ──
    public static readonly Color Primary = Color.FromArgb("#4F46E5");
    public static readonly Color PrimarySoft = Color.FromArgb("#EEF2FF");
    public static readonly Color Bg = Color.FromArgb("#F2F4FA");
    public static readonly Color Card = Color.FromArgb("#FFFFFF");
    public static readonly Color Border = Color.FromArgb("#E4E7F0");
    public static readonly Color Ink = Color.FromArgb("#1F2937");
    public static readonly Color Muted = Color.FromArgb("#6B7280");
    public static readonly Color Success = Color.FromArgb("#16A34A");
    public static readonly Color SuccessSoft = Color.FromArgb("#E9F7F0");
    public static readonly Color Danger = Color.FromArgb("#DC2626");
    public static readonly Color DangerSoft = Color.FromArgb("#FDECEC");
    public static readonly Color Warning = Color.FromArgb("#B45309");
    public static readonly Color WarningSoft = Color.FromArgb("#FDF1E0");

    // ── البطاقة ──
    public static Border CardView(params View[] children)
    {
        var inner = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(14) };
        foreach (var c in children) inner.Add(c);
        return new Border
        {
            BackgroundColor = Card,
            Stroke = Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = inner
        };
    }

    // ── النصوص ──
    public static Label H1(string text) => new Label { Text = text, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Ink };
    public static Label H2(string text) => new Label { Text = text, FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Ink };
    public static Label P(string text, int size = 14) => new Label { Text = text, FontSize = size, TextColor = Muted, LineBreakMode = LineBreakMode.WordWrap };
    public static Label Section(string text) => new Label { Text = text, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Muted };

    // ── الحقول ──
    public static Entry Input(string placeholder, string text = "")
    {
        return new Entry
        {
            Placeholder = placeholder,
            Text = text,
            FontSize = 15,
            TextColor = Ink,
            PlaceholderColor = Muted,
            BackgroundColor = Color.FromArgb("#F8FAFD"),
            MinimumHeightRequest = 42
        };
    }

    // ── الشرائط ──
    public static Border Chip(string text, Color bg, Color fg)
    {
        var label = new Label
        {
            Text = text,
            FontSize = 12,
            TextColor = fg,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        return new Border
        {
            BackgroundColor = bg,
            Stroke = fg,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
            Padding = new Thickness(10, 4),
            Content = label
        };
    }

    // ── الأزرار ──
    public static Button Filled(string text)
    {
        var b = new Button
        {
            Text = text,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Primary,
            CornerRadius = 12,
            BorderWidth = 0,
            HeightRequest = 48
        };
        b.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#18000000")),
            Offset = new Point(0, 2),
            Radius = 6
        };
        return b;
    }

    public static Button Ghost(string text)
    {
        return new Button
        {
            Text = text,
            FontSize = 15,
            TextColor = Primary,
            BackgroundColor = Card,
            CornerRadius = 10,
            BorderWidth = 1,
            BorderColor = Border,
            HeightRequest = 42
        };
    }

    public static Button BackButton() =>
        new Button
        {
            Text = "←  رجوع",
            FontSize = 14,
            TextColor = Primary,
            BackgroundColor = Card,
            CornerRadius = 10,
            BorderWidth = 1,
            BorderColor = Border,
            HeightRequest = 38
        };
}