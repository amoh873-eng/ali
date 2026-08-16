using System.Globalization;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Infrastructure;
using ERPSystem.Infrastructure.Data;
using ERPSystem.Web.Components;
using ERPSystem.Web.Middleware;
using ERPSystem.Web.Permissions;
using ERPSystem.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services for UI components
builder.Services.AddMudServices();

// Localization: دعم تبديل اللغة (عربي/إنجليزي) عبر IStringLocalizer + ملفات .resx
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = new[] { "ar", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// استخدم الكوكيز فقط لتحديد اللغة — تجاهل تفضيل لغة المتصفح (Accept-Language)
// حتى تظهر الواجهة بالعربية افتراضيًا، وتتغير يدويًا فقط من قائمة اللغة.
localizationOptions.RequestCultureProviders =
    new List<IRequestCultureProvider> { new CookieRequestCultureProvider() };

// Add infrastructure services (DbContext, AccountService, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// ASP.NET Core Identity: المصادقة والصلاحيات (Role-Based)
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<ERPSystem.Infrastructure.Data.AppDbContext>()
    .AddDefaultTokenProviders();

// التفويض بالصلاحيات: سياسات ديناميكية "Permission:*" + حقن أذونات الأدوار في هوية المستخدم
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AppUserClaimsPrincipalFactory>();
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();

// خدمات إدارة الأدوار والصلاحيات والمستخدمين
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISystemAdminService, SystemAdminService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

// Add Blazor interactive server components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// تسجيل الاستثناءات غير المعالَجة في قاعدة البيانات (لوحة إدارة النظام — المرحلة الأولى)
app.UseMiddleware<ExceptionLoggingMiddleware>();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRequestLocalization(localizationOptions);
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Export endpoints: تنزيل التقارير المالية بصيغة Excel
app.MapGet("/export/trial-balance/excel", async (IReportService reportService) =>
{
    var dto = await reportService.GetTrialBalanceAsync();
    return Results.File(ReportExporter.ExportTrialBalance(dto),
        ReportExporter.ExcelContentType, $"trial-balance-{DateTime.Now:yyyyMMdd}.xlsx");
});

app.MapGet("/export/income-statement/excel", async (IReportService reportService, DateTime? from, DateTime? to) =>
{
    var dto = await reportService.GetIncomeStatementAsync(
        from ?? new DateTime(DateTime.Today.Year, 1, 1), to ?? DateTime.Today);
    return Results.File(ReportExporter.ExportIncomeStatement(dto),
        ReportExporter.ExcelContentType, $"income-statement-{DateTime.Now:yyyyMMdd}.xlsx");
});

app.MapGet("/export/balance-sheet/excel", async (IReportService reportService, DateTime? asOf) =>
{
    var dto = await reportService.GetBalanceSheetAsync(asOf ?? DateTime.Today);
    return Results.File(ReportExporter.ExportBalanceSheet(dto),
        ReportExporter.ExcelContentType, $"balance-sheet-{DateTime.Now:yyyyMMdd}.xlsx");
});

// Export endpoints: تنزيل التقارير المالية بصيغة PDF
app.MapGet("/export/trial-balance/pdf", async (IReportService reportService) =>
{
    var dto = await reportService.GetTrialBalanceAsync();
    return Results.File(PdfExporter.ExportTrialBalance(dto),
        PdfExporter.PdfContentType, $"trial-balance-{DateTime.Now:yyyyMMdd}.pdf");
});

app.MapGet("/export/income-statement/pdf", async (IReportService reportService, DateTime? from, DateTime? to) =>
{
    var dto = await reportService.GetIncomeStatementAsync(
        from ?? new DateTime(DateTime.Today.Year, 1, 1), to ?? DateTime.Today);
    return Results.File(PdfExporter.ExportIncomeStatement(dto),
        PdfExporter.PdfContentType, $"income-statement-{DateTime.Now:yyyyMMdd}.pdf");
});

app.MapGet("/export/balance-sheet/pdf", async (IReportService reportService, DateTime? asOf) =>
{
    var dto = await reportService.GetBalanceSheetAsync(asOf ?? DateTime.Today);
    return Results.File(PdfExporter.ExportBalanceSheet(dto),
        PdfExporter.PdfContentType, $"balance-sheet-{DateTime.Now:yyyyMMdd}.pdf");
});

// تبديل اللغة: يضبط كوكيز الثقافة ثم يعيد التوجيه لنفس الصفحة
app.MapGet("/culture/set", (HttpContext context, string? culture, string? redirectUri) =>
{
    if (!string.IsNullOrWhiteSpace(culture))
    {
        var requestCulture = new RequestCulture(culture, culture);
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(requestCulture),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
    }
    var target = string.IsNullOrEmpty(redirectUri) ? "/" : redirectUri;
    return Results.LocalRedirect(target);
});

// Apply migrations + seed roles/admin user
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ERPSystem.Infrastructure.Data.AppDbContext>();
    dbContext.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    await SeedIdentityAsync(roleManager, userManager);
    await SeedHrAsync(dbContext);
}

app.Run();

// زرع دور Admin ومستخدم إداري افتراضي للاختبار (admin@erp.com / Admin@123)
static async Task SeedIdentityAsync(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
{
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    // دور المشرف الأعلى (لوحة إدارة النظام) — يُمنح يدويًا للمستخدمين عبر شاشة المستخدمين
    if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));

    // منح دور المسؤول جميع الأذونات (كي يعمل admin@erp.com دائمًا)
    var adminRole = await roleManager.FindByNameAsync("Admin");
    if (adminRole is not null)
    {
        var existing = await roleManager.GetClaimsAsync(adminRole);
        var existingValues = existing
            .Where(c => c.Type == PermissionCatalog.ClaimType)
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var permission in PermissionCatalog.AllPermissions())
        {
            if (!existingValues.Contains(permission))
                await roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim(PermissionCatalog.ClaimType, permission));
        }
    }

    // زرع الأدوار الجاهزة المقسمة حسب الموديولات (كاشير، مخازن، مبيعات، ...)
    foreach (var (roleName, moduleKey) in PermissionCatalog.ModuleRoles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));

        var moduleRole = await roleManager.FindByNameAsync(roleName);
        if (moduleRole is null) continue;

        var moduleExisting = (await roleManager.GetClaimsAsync(moduleRole))
            .Where(c => c.Type == PermissionCatalog.ClaimType)
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var action in PermissionCatalog.Actions)
        {
            var permission = PermissionCatalog.Build(moduleKey, action);
            if (!moduleExisting.Contains(permission))
                await roleManager.AddClaimAsync(moduleRole, new System.Security.Claims.Claim(PermissionCatalog.ClaimType, permission));
        }
    }

    if (await userManager.FindByEmailAsync("admin@erp.com") is null)
    {
        var admin = new IdentityUser { UserName = "admin@erp.com", Email = "admin@erp.com", EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}

// زرع الأقسام والمسميات الوظيفية القياسية (المبيعات، المحاسبة، المخازن، ...)
static async Task SeedHrAsync(AppDbContext dbContext)
{
    var departments = new (Guid Id, string Code, string NameAr, string NameEn)[]
    {
        (new Guid("60000000-0000-0000-0000-000000000002"), "SALES", "المبيعات", "Sales"),
        (new Guid("60000000-0000-0000-0000-000000000003"), "PURCHASES", "المشتريات", "Purchases"),
        (new Guid("60000000-0000-0000-0000-000000000004"), "WAREHOUSE", "المخازن", "Warehouse"),
        (new Guid("60000000-0000-0000-0000-000000000005"), "ACCOUNTING", "المحاسبة", "Accounting"),
        (new Guid("60000000-0000-0000-0000-000000000006"), "HR", "الموارد البشرية", "Human Resources"),
        (new Guid("60000000-0000-0000-0000-000000000007"), "CRM", "علاقات العملاء", "Customer Relations"),
        (new Guid("60000000-0000-0000-0000-000000000008"), "IT", "تقنية المعلومات", "Information Technology"),
    };

    foreach (var d in departments)
    {
        if (!await dbContext.Departments.AnyAsync(x => x.Code == d.Code))
        {
            dbContext.Departments.Add(new Department
            {
                Id = d.Id,
                Code = d.Code,
                NameAr = d.NameAr,
                NameEn = d.NameEn,
                IsActive = true,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    var positions = new (Guid Id, string Code, string NameAr, string NameEn, Guid DepartmentId)[]
    {
        (new Guid("70000000-0000-0000-0000-000000000002"), "SALES-REP", "مندوب مبيعات", "Sales Representative", new Guid("60000000-0000-0000-0000-000000000002")),
        (new Guid("70000000-0000-0000-0000-000000000003"), "PURCH-OFF", "مسؤول مشتريات", "Purchasing Officer", new Guid("60000000-0000-0000-0000-000000000003")),
        (new Guid("70000000-0000-0000-0000-000000000004"), "WH-STORE", "أمين مخزن", "Storekeeper", new Guid("60000000-0000-0000-0000-000000000004")),
        (new Guid("70000000-0000-0000-0000-000000000005"), "ACC-ACCT", "محاسب", "Accountant", new Guid("60000000-0000-0000-0000-000000000005")),
        (new Guid("70000000-0000-0000-0000-000000000006"), "HR-SPEC", "أخصائي موارد بشرية", "HR Specialist", new Guid("60000000-0000-0000-0000-000000000006")),
        (new Guid("70000000-0000-0000-0000-000000000007"), "CRM-CS", "موظف خدمة عملاء", "Customer Service", new Guid("60000000-0000-0000-0000-000000000007")),
        (new Guid("70000000-0000-0000-0000-000000000008"), "IT-SPEC", "أخصائي تقنية معلومات", "IT Specialist", new Guid("60000000-0000-0000-0000-000000000008")),
    };

    foreach (var p in positions)
    {
        if (!await dbContext.Positions.AnyAsync(x => x.Code == p.Code))
        {
            dbContext.Positions.Add(new Position
            {
                Id = p.Id,
                Code = p.Code,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                DepartmentId = p.DepartmentId,
                IsActive = true,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    await dbContext.SaveChangesAsync();
}
