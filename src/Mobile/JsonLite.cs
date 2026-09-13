using System.Text;

namespace ERPSystem.Mobile;

/// <summary>
/// محلّل JSON خفيف لِشكلنا المُتحكم فيه: كائنات مسطحة + قيم ASCII-مفروغة (\uXXXX).
/// لا يعتمد على JsonDocument (عاجز عن فك الترميز في هذا المُشغِّل).
/// </summary>
public static class JsonLite
{
    /// <summary>يفك \uXXXX ضمن قيمة مفرّغة إلى حرف Unicode (للعرض).</summary>
    public static string Unescape(string s)
    {
        var sb = new StringBuilder();
        var chars = s.ToCharArray();
        int i = 0;
        while (i < chars.Length)
        {
            var c = chars[i];
            if (c == '\\' && i + 5 < chars.Length && chars[i + 1] == 'u'
                && IsHex(chars[i + 2]) && IsHex(chars[i + 3]) && IsHex(chars[i + 4]) && IsHex(chars[i + 5]))
            {
                var v = (HexVal(chars[i + 2]) << 12) | (HexVal(chars[i + 3]) << 8)
                      | (HexVal(chars[i + 4]) << 4) | HexVal(chars[i + 5]);
                sb.Append((char)v);
                i += 6;
            }
            else { sb.Append(c); i++; }
        }
        return sb.ToString();
    }

    /// <summary>يستخرج قيمة مفتاح (سلسلة مسطحة أو رقم أو true/false) من كتلة {…}.</summary>
    public static string Field(string block, string name)
    {
        var token = "\"" + name + "\":";
        var i = block.IndexOf(token);
        if (i < 0) return "";
        var start = i + token.Length;
        if (start >= block.Length) return "";
        var c = block[start];
        if (c == '"')
        {
            var end = block.IndexOf("\"", start + 1);
            return end < 0 ? "" : block.Substring(start + 1, end - start - 1);
        }
        var j = start;
        while (j < block.Length && "0123456789-.eEtruefalsn".IndexOf(block[j]) >= 0) j++;
        return block.Substring(start, j - start);
    }

    /// <summary>يستخرج كائناً متداخلاً {…} لقيمة مفتاح.</summary>
    public static string ObjectField(string block, string name)
    {
        var token = "\"" + name + "\":";
        var i = block.IndexOf(token);
        if (i < 0) return "";
        var start = i + token.Length;
        while (start < block.Length && block[start] != '{') start++;
        if (start >= block.Length) return "";
        var depth = 0;
        var sb = new StringBuilder();
        var chars = block.ToCharArray();
        var inStr = false;
        for (int j = start; j < chars.Length; j++)
        {
            var c = chars[j];
            sb.Append(c);
            if (inStr)
            {
                if (c == '\\' && j + 1 < chars.Length) { sb.Append(chars[j + 1]); j++; }
                else if (c == '"') inStr = false;
                continue;
            }
            if (c == '"') { inStr = true; continue; }
            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) return sb.ToString();
            }
        }
        return "";
    }

    /// <summary>يستخرج مصفوفة [..] كاملة لقيمة مفتاح في كتلة مسطحة.</summary>
    public static string ArrayField(string block, string name)
    {
        var token = "\"" + name + "\":";
        var i = block.IndexOf(token);
        if (i < 0) return "";
        var start = i + token.Length;
        while (start < block.Length && block[start] != '[') start++;
        if (start >= block.Length) return "";
        var depth = 0;
        var sb = new StringBuilder();
        var chars = block.ToCharArray();
        bool inStr = false;
        for (int j = start; j < chars.Length; j++)
        {
            var c = chars[j];
            sb.Append(c);
            if (inStr)
            {
                if (c == '\\' && j + 5 < chars.Length && chars[j + 1] == 'u'
                    && IsHex(chars[j + 2]) && IsHex(chars[j + 3]) && IsHex(chars[j + 4]) && IsHex(chars[j + 5]))
                {
                    for (int k = 1; k <= 5; k++) sb.Append(chars[j + k]);
                    j += 5;
                }
                else if (c == '"') inStr = false;
                continue;
            }
            if (c == '"') { inStr = true; continue; }
            if (c == '[') depth++;
            else if (c == ']')
            {
                depth--;
                if (depth == 0) return sb.ToString();
            }
        }
        return "";
    }

    /// <summary>يقسم مصفوفة JSON إلى كتل كائنات مسطحة.</summary>
    public static List<string> Objects(string json)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        var depth = 0;
        bool inString = false;
        var chars = json.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (inString)
            {
                sb.Append(c);
                if (c == '\\' && i + 5 < chars.Length && chars[i + 1] == 'u'
                    && IsHex(chars[i + 2]) && IsHex(chars[i + 3]) && IsHex(chars[i + 4]) && IsHex(chars[i + 5]))
                {
                    for (int k = 1; k <= 5; k++) sb.Append(chars[i + k]);
                    i += 5;
                }
                else if (c == '"') inString = false;
                continue;
            }
            if (c == '"') { inString = true; sb.Append(c); continue; }
            if (c == '{') { depth++; sb.Append(c); continue; }
            if (c == '}')
            {
                depth--;
                sb.Append(c);
                if (depth == 0) { list.Add(sb.ToString()); sb = new StringBuilder(); }
                continue;
            }
            sb.Append(c);
        }
        return list;
    }

    private static bool IsHex(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    private static int HexVal(char c) => c <= '9' ? (c - '0') : (c <= 'F' ? (c - 'A' + 10) : (c - 'a' + 10));
}