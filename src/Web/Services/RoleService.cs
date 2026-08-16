using System.Security.Claims;
using ERPSystem.Web.Permissions;
using Microsoft.AspNetCore.Identity;

namespace ERPSystem.Web.Services;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UsersCount { get; set; }
    public int PermissionsCount { get; set; }
}

public interface IRoleService
{
    Task<List<RoleDto>> GetRolesAsync();
    Task CreateRoleAsync(string name);
    Task DeleteRoleAsync(Guid id);
    Task<List<string>> GetRolePermissionsAsync(Guid roleId);
    Task SetRolePermissionsAsync(Guid roleId, List<string> permissions);
}

/// <summary>
/// يدير الأدوار (IdentityRole) ومصفوفة صلاحياتها المخزَّنة كـ Claims.
/// </summary>
public class RoleService : IRoleService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public RoleService(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
        var result = new List<RoleDto>();

        foreach (var role in roles)
        {
            var users = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
            var claims = await _roleManager.GetClaimsAsync(role);

            result.Add(new RoleDto
            {
                Id = Guid.Parse(role.Id),
                Name = role.Name ?? string.Empty,
                UsersCount = users.Count,
                PermissionsCount = claims.Count(c => c.Type == PermissionCatalog.ClaimType)
            });
        }

        return result;
    }

    public async Task CreateRoleAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("اسم الدور مطلوب");

        if (await _roleManager.RoleExistsAsync(name))
            throw new InvalidOperationException("الدور موجود مسبقاً");

        await _roleManager.CreateAsync(new IdentityRole(name));
    }

    public async Task DeleteRoleAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            throw new InvalidOperationException("الدور غير موجود");

        if (role.Name == "Admin")
            throw new InvalidOperationException("لا يمكن حذف دور المسؤول");

        await _roleManager.DeleteAsync(role);
    }

    public async Task<List<string>> GetRolePermissionsAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            throw new InvalidOperationException("الدور غير موجود");

        var claims = await _roleManager.GetClaimsAsync(role);
        return claims.Where(c => c.Type == PermissionCatalog.ClaimType).Select(c => c.Value).ToList();
    }

    public async Task SetRolePermissionsAsync(Guid roleId, List<string> permissions)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            throw new InvalidOperationException("الدور غير موجود");

        var existing = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in existing.Where(c => c.Type == PermissionCatalog.ClaimType))
            await _roleManager.RemoveClaimAsync(role, claim);

        foreach (var permission in permissions.Distinct())
            await _roleManager.AddClaimAsync(role, new Claim(PermissionCatalog.ClaimType, permission));
    }
}
