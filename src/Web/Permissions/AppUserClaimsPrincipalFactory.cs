using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ERPSystem.Web.Permissions;

/// <summary>
/// يضخ أذونات الأدوار (Claims) في الهوية الرئيسية للمستخدم عند تسجيل الدخول،
/// حتى تُقيَّم سياسات "Permission:*" و AuthorizeView بشكل صحيح.
/// </summary>
public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<IdentityUser, IdentityRole>
{
    public AppUserClaimsPrincipalFactory(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(IdentityUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var roles = await UserManager.GetRolesAsync(user);
        foreach (var roleName in roles)
        {
            var role = await RoleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var claims = await RoleManager.GetClaimsAsync(role);
            identity.AddClaims(claims);
        }

        return identity;
    }
}
