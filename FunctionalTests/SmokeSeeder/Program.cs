using ERPSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// SmokeSeeder: creates a dedicated smoke-test admin (smoke@erp.com / Test@1234) in the MAIN
// app database (ERPSystemDb) without touching existing accounts/passwords. Used by smoke_admin.js
// to exercise the login -> dashboard flow end-to-end.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var conn = "Server=(localdb)\\mssqllocaldb;Database=ERPSystemDb;Trusted_Connection=True;TrustServerCertificate=True";
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options;
using var db = new AppDbContext(options);

var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<IdentityUser>(db);
var userManager = new UserManager<IdentityUser>(userStore, null!, new PasswordHasher<IdentityUser>(), null!, null!, null!, null!, null!, null!);
var roleStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<IdentityRole>(db);
var roleManager = new RoleManager<IdentityRole>(roleStore, null!, null!, null!, null!);

string[] moduleRoles = ["Sales", "Purchases", "Inventory", "Accounting", "Expenses", "Crm", "Hr", "Reports", "Pos", "Permissions"];

foreach (var rk in moduleRoles.Concat(["Admin", "SuperAdmin"]))
{
    if (!await roleManager.RoleExistsAsync(rk))
        await roleManager.CreateAsync(new IdentityRole(rk));
}

const string email = "smoke@erp.com";
var existing = await userManager.FindByEmailAsync(email);
if (existing is null)
{
    var u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
    var r = await userManager.CreateAsync(u, "Test@1234");
    Console.WriteLine($"{email}: create={r.Succeeded}");
    existing = await userManager.FindByEmailAsync(email);
}
else
{
    Console.WriteLine($"{email}: exists (password left unchanged)");
}

if (existing is not null)
{
    foreach (var rk in moduleRoles.Concat(["Admin"]))
    {
        if (!await userManager.IsInRoleAsync(existing, rk))
            await userManager.AddToRoleAsync(existing, rk);
    }
    Console.WriteLine($"{email}: roles ensured");
}

Console.WriteLine("DONE");