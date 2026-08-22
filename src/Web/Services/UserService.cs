using Microsoft.AspNetCore.Identity;

namespace ERPSystem.Web.Services;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync();
    Task<List<string>> GetUserRolesAsync(Guid userId);
    Task SetUserRolesAsync(Guid userId, List<string> roleNames);
    Task CreateUserAsync(string userName, string email, string password, List<string> roleNames);
    Task DeleteUserAsync(Guid userId);
}

/// <summary>
/// يدير تعيين الأدوار للمستخدمين.
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<IdentityUser> _userManager;

    public UserService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = _userManager.Users.OrderBy(u => u.UserName).ToList();
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = Guid.Parse(user.Id),
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            });
        }

        return result;
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new InvalidOperationException("المستخدم غير موجود");

        return (await _userManager.GetRolesAsync(user)).ToList();
    }

    public async Task SetUserRolesAsync(Guid userId, List<string> roleNames)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new InvalidOperationException("المستخدم غير موجود");

        var current = await _userManager.GetRolesAsync(user);
        if (current.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, current);
            if (!removeResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", removeResult.Errors.Select(e => e.Description)));
        }

        if (roleNames.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, roleNames);
            if (!addResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", addResult.Errors.Select(e => e.Description)));
        }
    }

    public async Task CreateUserAsync(string userName, string email, string password, List<string> roleNames)
    {
        var user = new IdentityUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        if (roleNames.Count > 0)
        {
            var addResult2 = await _userManager.AddToRolesAsync(user, roleNames);
            if (!addResult2.Succeeded)
                throw new InvalidOperationException(string.Join("; ", addResult2.Errors.Select(e => e.Description)));
        }
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new InvalidOperationException("المستخدم غير موجود");

        await _userManager.DeleteAsync(user);
    }
}
