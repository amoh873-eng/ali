using System.Globalization;
using ERPSystem.Application.Interfaces;
using ERPSystem.Infrastructure;
using ERPSystem.Web.Components;
using ERPSystem.Web.Services;
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
}

app.Run();

// زرع دور Admin ومستخدم إداري افتراضي للاختبار (admin@erp.com / Admin@123)
static async Task SeedIdentityAsync(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
{
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (await userManager.FindByEmailAsync("admin@erp.com") is null)
    {
        var admin = new IdentityUser { UserName = "admin@erp.com", Email = "admin@erp.com", EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}
