using ERPSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// UserSeeder: creates the 9 single-role module test users + ensures admin exists.
// Uses the real Identity API (same path the app UI uses), NOT direct SQL.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var conn = "Server=(localdb)\\mssqllocaldb;Database=ERPSystemDb_FuncTest;Trusted_Connection=True;TrustServerCertificate=True";
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options;
using var db = new AppDbContext(options);
var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<IdentityUser>(db);
var userManager = new UserManager<IdentityUser>(userStore, null!, new PasswordHasher<IdentityUser>(), null!, null!, null!, null!, null!, null!);
var roleStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<IdentityRole>(db);
var roleManager = new RoleManager<IdentityRole>(roleStore, null!, null!, null!, null!);

var pass = "Test@1234";
var users = new (string email, string role)[]
{
    ("sales@test.local", "Sales"),
    ("purchases@test.local", "Purchases"),
    ("inventory@test.local", "Inventory"),
    ("accounting@test.local", "Accounting"),
    ("hr@test.local", "Hr"),
    ("crm@test.local", "Crm"),
    ("expenses@test.local", "Expenses"),
    ("reports@test.local", "Reports"),
    ("pos@test.local", "Pos"),
    ("all@test.local", "Admin"),
};

foreach (var (email, role) in users)
{
    var existing = await userManager.FindByEmailAsync(email);
    if (existing is null)
    {
        var u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var r = await userManager.CreateAsync(u, pass);
        Console.WriteLine($"{email}: create={r.Succeeded}");
    }
    else
    {
        Console.WriteLine($"{email}: exists");
    }
    var byEmail = await userManager.FindByEmailAsync(email);
    if (byEmail is not null && !await userManager.IsInRoleAsync(byEmail, role))
    {
        var ar = await userManager.AddToRoleAsync(byEmail, role);
        Console.WriteLine($"{email}: addRole({role})={ar.Succeeded}");
    }
}
Console.WriteLine("DONE");