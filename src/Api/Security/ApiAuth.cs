using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ERPSystem.Api.Security;

/// <summary>المستخدم الجاري تحقق منه من التوكن (يُقرأ من قاعدة الهوية نفسها — إعادة استخدام UserManager).</summary>
public record ApiUser(string Id, string Name, IEnumerable<string> Roles);

/// <summary>
/// حارس المسارات المحمية: يستخرج التوكن من ترويسة Authorization، يتحقق من إمضائه/انتهائه
/// (JwtService)، ثم يقرأ بيانات المستخدم الحيّة من هوية النظام (UserManager/RoleManager).
/// </summary>
public class ApiAuth
{
    /// <summary>يرجع المستخدم الموثّق أو null (عندها يضع الحارس 401).</summary>
    public static async Task<ApiUser?> RequireUserAsync(HttpContext ctx, JwtService jwt, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        var header = ctx.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = header.Substring(7).Trim();
        var subject = jwt.VerifySubject(token);
        if (string.IsNullOrEmpty(subject))
            return null;

        var user = await userManager.FindByIdAsync(subject);
        if (user is null)
            return null;

        var roles = await userManager.GetRolesAsync(user);
        var name = user.UserName ?? user.Email ?? subject;
        return new ApiUser(subject, name, roles);
    }
}