using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace ERPSystem.Web.Permissions;

/// <summary>
/// يحقن أذونات الأدوار (Claims) في هوية المستخدم عند كل طلب،
/// حتى تظهر الصلاحيات فورًا دون الحاجة لتسجيل خروج/دخول بعد تعديلها.
/// </summary>
public class PermissionClaimsTransformation : IClaimsTransformation
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public PermissionClaimsTransformation(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        var userName = identity.Name;
        if (string.IsNullOrEmpty(userName))
            return principal;

        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
            return principal;

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
            {
                if (!identity.HasClaim(claim.Type, claim.Value))
                    identity.AddClaim(claim);
            }
        }

        return principal;
    }
}
