using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace ERPSystem.Api.Security;

/// <summary>
/// إصدار/تحقق JWT (HS256) عبر مكتبات Microsoft.IdentityModel الموجودة أصلاً في المشروع.
/// لا نظام مستخدمين موازٍ — التوثيق عبر SignInManager، والتحقق يقرأ المستخدم من هوية
/// النظام عبر UserManager. السر في الإعداد نص سادس-عشري (hex) عبر Convert.FromHexString.
/// </summary>
public class JwtService
{
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;

    public JwtService(string secretHex, string issuer)
    {
        _key = new SymmetricSecurityKey(Convert.FromHexString(secretHex));
        _issuer = issuer;
    }

    public string Issue(string subject, int hours)
    {
        var now = DateTime.UtcNow;
        var identity = new ClaimsIdentity([new Claim("sub", subject), new Claim("expIso", now.AddHours(hours).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"))]);
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);
        return new JwtSecurityTokenHandler()
            .CreateEncodedJwt(_issuer, subject, identity, now, now.AddHours(hours), null, creds)
            .ToString();
    }

    /// <summary>subject عند صحة الإمضاء وعدم الانتهاء وتطابق iss؛ وإلا null.</summary>
    public string? VerifySubject(string token)
    {
        try
        {
            if (token is null) return null;
            var parts = token.Split(".");
            if (parts.Length != 3) return null;

            var signingInput = parts[0] + "." + parts[1];
            var signer = new SymmetricSignatureProvider(_key, SecurityAlgorithms.HmacSha256Signature);
            var expected = signer.Sign(AsciiBytes(signingInput));
            var actual = UrlDecode64(parts[2]);
            if (expected is null || !ConstantTimeEquals(expected, actual)) return null;

            var payloadJson = FromUtf8Bytes(UrlDecode64(parts[1]));
            var payload = System.Text.Json.JsonDocument.Parse(payloadJson).RootElement;
            if (!payload.TryGetProperty("iss", out var issNode) || issNode.GetString() != _issuer) return null;

            var subject = payload.TryGetProperty("sub", out var subNode) ? subNode.GetString() : null;
            if (string.IsNullOrEmpty(subject)) return null;

            // التحقق الزمني عبر claim expIso (سلسلة ISO-UTC ثابتة الطول — المقارنة معجمياً صحيحة)
            if (payload.TryGetProperty("expIso", out var expNode))
            {
                var isoNow = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
                if (expNode.GetString().CompareTo(isoNow) <= 0) return null;
            }
            else return null;

            return subject;
        }
        catch { return null; }
    }

    private static byte[] UrlDecode64(string input)
    {
        var std = input.Replace('-', '+').Replace('_', '/');
        while (std.Length % 4 != 0) std += "=";
        return Convert.FromBase64String(std);
    }

    private static bool ConstantTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }

    private static byte[] AsciiBytes(string s)
    {
        var chars = s.ToCharArray();
        var bytes = new byte[chars.Length];
        for (int i = 0; i < chars.Length; i++) bytes[i] = (byte)chars[i];
        return bytes;
    }

    private static string FromUtf8Bytes(byte[] b)
    {
        var chars = new char[b.Length];
        for (int i = 0; i < b.Length; i++) chars[i] = (char)(b[i] & 0xFF);
        return new String(chars);
    }
}