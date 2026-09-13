using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace ERPSystem.Mobile;

/// <summary>
/// هوية بصرية موحّدة: بنفسجية #6D5BD0 (نفس هوية Blazor دون نسخها)،
/// عناوين ملونة، أزرار لمس كبيرة بزوايا دائرية، فواصل، وإشعارات مصفّأة.
/// </summary>
public static class Theme
{
    // SlateBlue ≈ #6A5ACD — شبه مطابق لبنفسجية #6D5BD0 المستخدمة في بلازور
    public static readonly Color Primary = Microsoft.Maui.Graphics.Colors.SlateBlue;
    public static readonly Color Success = Microsoft.Maui.Graphics.Colors.SeaGreen;
    public static readonly Color Danger = Microsoft.Maui.Graphics.Colors.Crimson;
    public static readonly Color Ink = Microsoft.Maui.Graphics.Colors.Black;

    /// <summary>عنوان شاشة بنفسجية.</summary>
    public static Label Title(string text, int size)
    {
        var l = new Label();
        l.Text = text;
        l.FontSize = size;
        l.TextColor = Primary;
        return l;
    }

    /// <summary>فاصل أفقي خفيف.</summary>
    public static Label Rule()
    {
        var l = new Label();
        l.Text = "───────────────────────────────";
        l.TextColor = Ink;
        return l;
    }

    /// <summary>زر رئيسي كبير مناسب للمس.</summary>
    public static Button Button(String text)
    {
        var b = new Button();
        b.Text = text;
        b.FontSize = 16;
        b.TextColor = Ink;
        b.CornerRadius = 12;
        b.BorderWidth = 2;
        b.BorderColor = Primary;
        return b;
    }

    /// <summary>زر ثانوي مكتوب بلون المخزون.</summary>
    public static Button GhostButton(String text)
    {
        var b = Button(text);
        b.BorderColor = Microsoft.Maui.Graphics.Colors.LightGray;
        return b;
    }

    /// <summary>إشعار نجاح/خطأ مصفّأ (Toast-like).</summary>
    public static Label Toast(String text, bool ok)
    {
        var l = new Label();
        l.Text = (ok ? "✓  " : "✕  ") + text;
        l.FontSize = 15;
        l.TextColor = ok ? Success : Danger;
        return l;
    }
}