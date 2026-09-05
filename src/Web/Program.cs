using System.Globalization;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Infrastructure;
using ERPSystem.Infrastructure.Data;
using ERPSystem.Infrastructure.Services;
using ERPSystem.Web.Components;
using ERPSystem.Web.Middleware;
using ERPSystem.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Sprint from build output (dotnet ERPSystem.Web.dll or the ERPWeb scheduled task) in Development:
// enables Static Web Assets so _framework + wwwroot + RCL assets (MudBlazor) are actually served.
// Without this, blazor.web.js / _content/* are returned as empty 200s and the interactive UI never loads.
if (builder.Environment.IsDevelopment())
    builder.WebHost.UseStaticWebAssets();

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

// Non-invasive scale-barcode interceptor settings (appsettings.json → IOptions).
builder.Services.Configure<ERPSystem.Web.Configuration.ScaleBarcodeSettings>(
    builder.Configuration.GetSection(ERPSystem.Web.Configuration.ScaleBarcodeSettings.SectionName));

// ASP.NET Core Identity: المصادقة والصلاحيات (Role-Based)
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 10;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<ERPSystem.Infrastructure.Data.AppDbContext>()
    .AddDefaultTokenProviders();

// خدمات إدارة الأدوار والمستخدمين (+ إدارة النظام)
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISystemAdminService, SystemAdminService>();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddHostedService<RenewalReminderService>();
builder.Services.AddHostedService<TrialExpiryService>();
builder.Services.AddHostedService<NotificationDigestService>();
builder.Services.AddHostedService<LowStockNotificationService>();
builder.Services.AddHostedService<ExpiryNotificationService>();
builder.Services.AddScoped<ErrorNotifierService>();
builder.Services.AddScoped<UpdateService>();

// Part G: health checks — use simple manual checks to avoid extra package; detailed check via /health/detailed uses these types
builder.Services.AddHealthChecks()
    .AddCheck<ERPSystem.Infrastructure.Health.DiskSpaceHealthCheck>("disk")
    .AddCheck<ERPSystem.Infrastructure.Health.SqlServiceHealthCheck>("sql")
    .AddCheck<ERPSystem.Infrastructure.Health.BackupHealthCheck>("backup")
    .AddCheck<ERPSystem.Infrastructure.Health.ErrorRateHealthCheck>("errors")
    .AddCheck<ERPSystem.Infrastructure.Health.SslExpiryHealthCheck>("ssl");

// ── Owner authentication — separate cookie scheme (never Identity) ──
builder.Services.AddAuthentication()
    .AddCookie("OwnerScheme", options =>
    {
        options.Cookie.Name = "OwnerAuth";
        options.LoginPath = "/owner-login-redirect-marker";
        options.AccessDeniedPath = "/owner-login-redirect-marker";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 404; return Task.CompletedTask; },
            OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 404; return Task.CompletedTask; }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("OwnerOnly", policy => policy.AddAuthenticationSchemes("OwnerScheme").RequireAuthenticatedUser());
});

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

// Owner IP gate — returns 404 if disallowed (defense-in-depth, hides route)
app.Use(async (ctx, next) =>
{
    var sec = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
    if (OwnerMiddlewareHelpers.IsOwnerPath(ctx, sec))
    {
        var allowed = ctx.RequestServices.GetRequiredService<IConfiguration>()["Owner:AllowedIPs"] ?? ctx.RequestServices.GetRequiredService<IConfiguration>()["Owner__AllowedIPs"];
        if (!OwnerMiddlewareHelpers.IsIpAllowed(ctx, allowed))
        {
            ctx.Response.StatusCode = 404;
            await ctx.Response.WriteAsync("Not Found");
            return;
        }
    }
    await next();
});

// تسجيل الاستثناءات غير المعالَجة في قاعدة البيانات (لوحة إدارة النظام — المرحلة الأولى)
app.UseMiddleware<ExceptionLoggingMiddleware>();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRequestLocalization(localizationOptions);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Owner 404-masking for unauthenticated owner paths
app.Use(async (ctx, next) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    var sec = OwnerMiddlewareHelpers.GetOwnerSecretPath(cfg);
    if (OwnerMiddlewareHelpers.IsOwnerPath(ctx, sec))
    {
        var p = ctx.Request.Path.Value ?? "";
        var isLogin = p.Equals("/" + sec + "/login", StringComparison.OrdinalIgnoreCase)
                   || p.Equals("/" + sec + "/login/handler", StringComparison.OrdinalIgnoreCase)
                   || p.Equals("/" + sec + "/enter", StringComparison.OrdinalIgnoreCase)
                   || p.Equals("/" + sec, StringComparison.OrdinalIgnoreCase)
                   || p.Equals("/" + sec + "/", StringComparison.OrdinalIgnoreCase);
        if (!isLogin)
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true || ar.Principal?.Identity?.IsAuthenticated != true)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync("Not Found");
                return;
            }
        }
    }
    await next();
});

app.UseMiddleware<DeploymentActiveMiddleware>();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Owner console (hidden path)
ERPSystem.Web.Owner.OwnerLoginEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerDashboardEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerBrandingEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerFeatureEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerSupportEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerSubscriptionEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerCustomReportEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerCustomReportEndpointsB.Map(app);
ERPSystem.Web.Owner.OwnerFinancialsEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerHistoryEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerHealthEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerUpdateEndpoints.Map(app);
ERPSystem.Web.Owner.OwnerTrialEndpoints.Map(app);

// Health endpoints: /health/ping anonymous, /health/detailed owner-only
app.MapHealthChecks("/health/ping", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions{ Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions{ Predicate = _ => true, ResponseWriter = async (ctx, report) =>
{
    ctx.Response.ContentType = "application/json; charset=utf-8";
    var json = System.Text.Json.JsonSerializer.Serialize(new{ status=report.Status.ToString(), checks=report.Entries.ToDictionary(kv=>kv.Key, kv=>new{ status=kv.Value.Status.ToString(), desc=kv.Value.Description, duration=kv.Value.Duration.TotalMilliseconds }), totalDuration=report.TotalDuration.TotalMilliseconds });
    await ctx.Response.WriteAsync(json);
}}).RequireAuthorization("OwnerOnly");

// Export endpoints: تنزيل التقارير المالية بصيغة Excel — مقيّد بدور Reports (+ Admin/SuperAdmin يحملانه ضمنيًا)
app.MapGet("/export/trial-balance/excel", async (IReportService reportService) =>
{
    var dto = await reportService.GetTrialBalanceAsync();
    return Results.File(ReportExporter.ExportTrialBalance(dto),
        ReportExporter.ExcelContentType, $"trial-balance-{DateTime.Now:yyyyMMdd}.xlsx");
}).RequireAuthorization(p => p.RequireRole("Reports", "Admin", "SuperAdmin"));

app.MapGet("/export/income-statement/excel", async (IReportService reportService, DateTime? from, DateTime? to) =>
{
    var dto = await reportService.GetIncomeStatementAsync(
        from ?? new DateTime(DateTime.Today.Year, 1, 1), to ?? DateTime.Today);
    return Results.File(ReportExporter.ExportIncomeStatement(dto),
        ReportExporter.ExcelContentType, $"income-statement-{DateTime.Now:yyyyMMdd}.xlsx");
}).RequireAuthorization(p => p.RequireRole("Reports", "Admin", "SuperAdmin"));

app.MapGet("/export/balance-sheet/excel", async (IReportService reportService, DateTime? asOf) =>
{
    var dto = await reportService.GetBalanceSheetAsync(asOf ?? DateTime.Today);
    return Results.File(ReportExporter.ExportBalanceSheet(dto),
        ReportExporter.ExcelContentType, $"balance-sheet-{DateTime.Now:yyyyMMdd}.xlsx");
}).RequireAuthorization(p => p.RequireRole("Reports", "Admin", "SuperAdmin"));

// Export endpoints: تنزيل التقارير المالية بصيغة PDF
app.MapGet("/export/trial-balance/pdf", async (IReportService reportService) =>
{
    var dto = await reportService.GetTrialBalanceAsync();
    return Results.File(PdfExporter.ExportTrialBalance(dto),
        PdfExporter.PdfContentType, $"trial-balance-{DateTime.Now:yyyyMMdd}.pdf");
}).RequireAuthorization(p => p.RequireRole("Reports", "Admin", "SuperAdmin"));

app.MapGet("/export/income-statement/pdf", async (IReportService reportService, DateTime? from, DateTime? to) =>
{
    var dto = await reportService.GetIncomeStatementAsync(
        from ?? new DateTime(DateTime.Today.Year, 1, 1), to ?? DateTime.Today);
    return Results.File(PdfExporter.ExportIncomeStatement(dto),
        PdfExporter.PdfContentType, $"income-statement-{DateTime.Now:yyyyMMdd}.pdf");
}).RequireAuthorization(p => p.RequireRole("Reports", "Admin", "SuperAdmin"));

app.MapGet("/export/balance-sheet/pdf", async (IReportService reportService, DateTime? asOf) =>
{
    var dto = await reportService.GetBalanceSheetAsync(asOf ?? DateTime.Today);
    return Results.File(PdfExporter.ExportBalanceSheet(dto),
        PdfExporter.PdfContentType, $"balance-sheet-{DateTime.Now:yyyyMMdd}.pdf");
}).RequireAuthorization(p => p.RequireRole("Reports", "Admin", "SuperAdmin"));

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

// تسجيل الدخول: نقطة نهاية Minimal API عادية بدلًا من مسار معالجة فورم Blazor SSR.
// يتجنّب هذا خطأ "The POST request does not specify which form is being submitted"
// نهائيًا، مع الإبقاء على حماية CSRF عبر RequireAntiforgeryTokenAttribute (النموذج الجديد
// في .NET 10 الذي حلّ محل الامتداد القديم RequireAntiforgery()): يتحقق وسيط UseAntiforgery()
// تلقائيًا من التوكن الذي يقدّمه مكوّن <AntiforgeryToken /> في نموذج تسجيل الدخول.
// ملاحظة: لا نستخدم نفس المسار "/login" الذي يشغله مكوّن Razor @page "/login" (الذي يطابق
// كل طرق HTTP ويثير AmbiguousMatchException مع MapPost). لذلك نستخدم مسارًا منفصلًا
// "/login/handler" ويستهدفه <form action="/login/handler"> في Login.razor.
app.MapGet("/login/handler", () => Results.Redirect("/login")).AllowAnonymous();

app.MapPost("/login/handler", async (HttpContext context, SignInManager<IdentityUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();
    var rememberMe = form["RememberMe"].ToString() == "true";
    var returnUrl = form["ReturnUrl"].ToString();
    var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    // Prevent redirect-loop: never redirect back to /login/handler itself
    if (target.Contains("/login/handler", StringComparison.OrdinalIgnoreCase))
        target = "/";

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        return Results.LocalRedirect("/login?error=1");

    var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);
    if (!result.Succeeded)
        return Results.LocalRedirect("/login?error=1");

    return Results.LocalRedirect(target);
}).WithMetadata(new RequireAntiforgeryTokenAttribute()).AllowAnonymous();

// Apply migrations + seed roles/admin user
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ERPSystem.Infrastructure.Data.AppDbContext>();
    dbContext.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // Seed the default admin only when a password is supplied (config/env).
    // In Development we fall back to "Admin@1234"; in Production no admin is created unless a password is configured explicitly.
    var seedAdminPassword = builder.Configuration["SeedAdmin:Password"];
    if (string.IsNullOrWhiteSpace(seedAdminPassword) && app.Environment.IsDevelopment())
        seedAdminPassword = "Admin@1234";
    await SeedIdentityAsync(roleManager, userManager, seedAdminPassword, dbContext);
    await SeedHrAsync(dbContext);
}

app.Run();

static async Task SeedIdentityAsync(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, string? adminPassword, AppDbContext dbContext)
{
    // نظام الأدوار الجديد: role = module (مفتاح إنجليزي)، وصول كامل للموديول بدون تفصيل View/Add/Edit/Delete.
    // Admin و SuperAdmin يحملان كل أدوار الموديولات ضمنيًا فلا حاجة لمنطق تجاوز خاص.
    string[] ModuleRoles = new[]
    {
        "Sales", "Purchases", "Inventory", "Accounting", "Expenses", "Crm", "Hr", "Reports", "Pos", "Permissions"
    };
    Dictionary<string, string> OldArabicToNewKey = new()
    {
        ["مبيعات"] = "Sales",
        ["مشتريات"] = "Purchases",
        ["مخازن"] = "Inventory",
        ["محاسبة"] = "Accounting",
        ["مصاريف"] = "Expenses",
        ["علاقات عملاء"] = "Crm",
        ["موارد بشرية"] = "Hr",
        ["تقارير"] = "Reports",
        ["كاشير"] = "Pos",
        ["صلاحيات"] = "Permissions",
    };
    // --- تنظيف وهجرة قاعدة بيانات قديمة (idempotent): حذف claims القديمة + نقل المستخدمين من أدوار عربية قديمة ---
    try
    {
        // احذف أي claims من نوع "Permission" الموروثة من النظام القديم
        var orphanClaims = dbContext.Database.ExecuteSqlRaw("DELETE FROM \"AspNetRoleClaims\" WHERE \"ClaimType\" = 'Permission'");
    }
    catch { /* قد لا يوجد الجدول في DB جديدة/اختبارات — تجاهل */ }

    foreach (var (oldName, newKey) in OldArabicToNewKey)
    {
        var oldRole = await roleManager.FindByNameAsync(oldName);
        if (oldRole is null) continue;

        // تأكد أن الدور الجديد موجود قبل النقل
        if (!await roleManager.RoleExistsAsync(newKey))
            await roleManager.CreateAsync(new IdentityRole(newKey));

        var usersInOld = await userManager.GetUsersInRoleAsync(oldName);
        foreach (var u in usersInOld)
        {
            if (!await userManager.IsInRoleAsync(u, newKey))
                await userManager.AddToRoleAsync(u, newKey);
            await userManager.RemoveFromRoleAsync(u, oldName);
        }

        // احذف الدور العربي القديم بعد نقل كل مستخدميه
        var stillHasUsers = (await userManager.GetUsersInRoleAsync(oldName)).Count > 0;
        if (!stillHasUsers)
        {
            // احذف أي claims متبقية على الدور القديم ثم الدور نفسه
            var claims = await roleManager.GetClaimsAsync(oldRole);
            foreach (var c in claims) await roleManager.RemoveClaimAsync(oldRole, c);
            await roleManager.DeleteAsync(oldRole);
        }
    }

    // --- زرع الأدوار الإنجليزية للموديولات ---
    foreach (var roleKey in ModuleRoles)
    {
        if (!await roleManager.RoleExistsAsync(roleKey))
            await roleManager.CreateAsync(new IdentityRole(roleKey));
    }

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));

    // امنح admin@erp.com كل أدوار الموديولات (إن وُجد) حتى يرى كل شيء دون منطق خاص
    var adminUser = await userManager.FindByEmailAsync("admin@erp.com");
    if (adminUser is not null)
    {
        foreach (var rk in ModuleRoles)
            if (!await userManager.IsInRoleAsync(adminUser, rk))
                await userManager.AddToRoleAsync(adminUser, rk);
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // إنشاء admin@erp.com إن لم يكن موجودًا (محمي بكلمة سر من الإعدادات)
    if (!string.IsNullOrWhiteSpace(adminPassword) &&
        await userManager.FindByEmailAsync("admin@erp.com") is null)
    {
        var admin = new IdentityUser { UserName = "admin@erp.com", Email = "admin@erp.com", EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            foreach (var rk in ModuleRoles)
                await userManager.AddToRoleAsync(admin, rk);
        }
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
