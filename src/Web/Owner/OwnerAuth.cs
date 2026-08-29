using System.Collections.Concurrent;
using System.Security.Claims;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace ERPSystem.Web.Owner;

public static class OwnerAuth
{
    public static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> Attempts = new();

    public static bool Verify(string username, string password, IConfiguration cfg)
    {
        var expectedUser = cfg["Owner:Username"] ?? cfg["Owner__Username"] ?? "owner";
        var expectedHash = cfg["Owner:PasswordHash"] ?? cfg["Owner__PasswordHash"];
        if (string.IsNullOrWhiteSpace(expectedHash)) return false;
        try
        {
            var hasher = new PasswordHasher<object>();
            var vr = hasher.VerifyHashedPassword(new object(), expectedHash, password);
            // SuccessRehashNeeded = كلمة المرور صحيحة لكن التجزئة بصيغة قديمة (v3).
            // بدون القبول بها كان الدخول يفشل حتى ببيانات صحيحة تماماً.
            return username == expectedUser &&
                   (vr == PasswordVerificationResult.Success ||
                    vr == PasswordVerificationResult.SuccessRehashNeeded);
        }
        catch { return false; }
    }

    public static async Task SignInAsync(HttpContext ctx, string username)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, "Owner") };
        await ctx.SignInAsync("OwnerScheme", new ClaimsPrincipal(new ClaimsIdentity(claims, "OwnerScheme")));
    }
}
