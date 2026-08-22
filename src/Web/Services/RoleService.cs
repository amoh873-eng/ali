using Microsoft.AspNetCore.Identity;

namespace ERPSystem.Web.Services;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UsersCount { get; set; }
}

public interface IRoleService
{
    Task<List<RoleDto>> GetRolesAsync();
    Task CreateRoleAsync(string name);
    Task DeleteRoleAsync(Guid id);
}

/// <summary>
/// يدير الأدوار (IdentityRole) بنظام role = module البسيط. لا توجد claims/أذونات منفصلة.
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

            result.Add(new RoleDto
            {
                Id = Guid.Parse(role.Id),
                Name = role.Name ?? string.Empty,
                UsersCount = users.Count
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

        var result = await _roleManager.CreateAsync(new IdentityRole(name));
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task DeleteRoleAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            throw new InvalidOperationException("الدور غير موجود");

        if (role.Name == "Admin" || role.Name == "SuperAdmin")
            throw new InvalidOperationException("لا يمكن حذف دور المسؤول");

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
