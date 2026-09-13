using System.Text;

namespace ERPSystem.Mobile;

/// <summary>
/// أدوات نصية: ترميز UTF-8 يدوي وبناء JSON بأحرف ASCII آمنة
/// (البايتات المرسلة نص ASCII خالص — يُتَرجَم أي حرف غير لاتيني إلى \uXXXX).
/// </summary>
public static class Text
{
    public static string EmptyIfNull(string? s) => s is null ? "" : s;

    /// <summary>يهرب سلسلة JSON: خطوط مائلة، اقتباسات، تحكم، وأي حرف غير ASCII → \uXXXX.</summary>
    public static string Json(string s)
    {
        var chars = s.ToCharArray();
        var sb = new StringBuilder();
        foreach (var c in chars)
        {
            var cp = (int)c;
            if (c == '\\') sb.Append("\\\\");
            else if (c == '"') sb.Append("\\\"");
            else if (cp == 0x0A) sb.Append("\\n");
            else if (cp == 0x0D) sb.Append("\\r");
            else if (cp == 0x09) sb.Append("\\t");
            else if (cp < 0x20) sb.Append("\\u00").Append(Hex2(cp));
            else if (cp < 0x80) sb.Append(c);
            else
            {
                sb.Append("\\u").Append(Hex(cp >> 12)).Append(Hex((cp >> 8) & 0xF))
                    .Append(Hex((cp >> 4) & 0xF)).Append(Hex(cp & 0xF));
            }
        }
        return sb.ToString();
    }

    private static char Hex(int v) => (char)(v < 10 ? '0' + v : 'a' + (v - 10));
    private static string Hex2(int v) => "" + Hex((v >> 4) & 0xF) + Hex(v & 0xF);

    /// <summary>ترميز UTF-8 سليم (يدعم العربية) للملفات والحمولات المحلية.</summary>
    public static byte[] Utf8Encode(string s)
    {
        var chars = s.ToCharArray();
        var buf = new List<byte>();
        foreach (var c in chars)
        {
            var cp = (int)c;
            if (cp < 0x80) buf.Add((byte)cp);
            else if (cp < 0x800) { buf.Add((byte)(0xC0 | (cp >> 6))); buf.Add((byte)(0x80 | (cp & 0x3F))); }
            else { buf.Add((byte)(0xE0 | (cp >> 12))); buf.Add((byte)(0x80 | ((cp >> 6) & 0x3F))); buf.Add((byte)(0x80 | (cp & 0x3F))); }
        }
        var bytesOut = new byte[buf.Count];
        for (int i = 0; i < buf.Count; i++) bytesOut[i] = buf[i];
        return bytesOut;
    }

    /// <summary>قراءة ملف ثنائي كاملاً (لإرفاق صورة من الجهاز).</summary>
    public static byte[] ReadBinaryFile(string path)
    {
        try
        {
            var f = File.OpenRead(path);
            var len = (int)f.Length;
            if (len <= 0) { f.Dispose(); return new byte[0]; }
            var bytes = new byte[len];
            var read = f.Read(bytes, 0, len);
            f.Dispose();
            return read == len ? bytes : new byte[0];
        }
        catch { return new byte[0]; }
    }

    /// <summary>بايتات ASCII للنص (بعد Text.Json يصبح النص ASCII خالصاً).</summary>
    public static byte[] AsciiBytes(string s)
    {
        var chars = s.ToCharArray();
        var bytes = new byte[chars.Length];
        for (int i = 0; i < chars.Length; i++) bytes[i] = (byte)chars[i];
        return bytes;
    }

    /// <summary>فك UTF-8 سليم (يدعم العربية) لقراءة ردود الـ API.</summary>
    public static string Utf8Decode(byte[] b)
    {
        var sb = new StringBuilder();
        int i = 0;
        while (i < b.Length)
        {
            int b0 = b[i] & 0xFF;
            if (b0 < 0x80) { sb.Append((char)b0); i++; }
            else if ((b0 & 0xE0) == 0xC0 && i + 1 < b.Length)
            {
                sb.Append((char)(((b0 & 0x1F) << 6) | (b[i + 1] & 0x3F))); i += 2;
            }
            else if ((b0 & 0xF0) == 0xE0 && i + 2 < b.Length)
            {
                sb.Append((char)(((b0 & 0x0F) << 12) | ((b[i + 1] & 0x3F) << 6) | (b[i + 2] & 0x3F))); i += 3;
            }
            else { sb.Append((char)b0); i++; }
        }
        return sb.ToString();
    }
}