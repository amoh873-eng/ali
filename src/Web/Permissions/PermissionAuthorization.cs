using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ERPSystem.Web.Permissions;

/// <summary>
/// متطلب تفويض لإذن واحد (قيمة مثل "Sales.View").
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// يتحقق أن المستخدم يملك الـ Claim "Permission:{value}" (تُضاف من أدواره عبر مصنع الـ Claims).
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(PermissionCatalog.ClaimType, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>
/// مزوّد سياسات ديناميكي: أي اسم سياسة يبدأ بـ "Permission:" يُحوَّل إلى متطلب إذن.
/// يتيح استخدام [Authorize(Policy = "Permission:Pos.View")] دون تسجيل كل سياسة يدويًا.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionCatalog.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PermissionCatalog.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
