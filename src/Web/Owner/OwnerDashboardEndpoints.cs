using ERPSystem.Application.Interfaces;
using ERPSystem.Infrastructure.Data;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Web.Owner;

public static class OwnerDashboardEndpoints
{
    public static void Map(WebApplication app)
    {
        var secret = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var prefix = "/" + secret;

        async Task WriteDashboard(HttpContext ctx)
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true || ar.Principal?.Identity?.IsAuthenticated != true)
            { ctx.Response.StatusCode = 404; await ctx.Response.WriteAsync("Not Found"); return; }
            using var scope = ctx.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            var settings = await svc.GetAsync();
            var empCount = 0; var invCount = 0; string lastLogin = "-";
            try { empCount = await db.Employees.CountAsync(); } catch { }
            try { invCount = await db.SalesInvoices.CountAsync(); } catch { }
            try { var u = await db.Users.OrderByDescending(x => x.Id).FirstOrDefaultAsync(); lastLogin = u?.UserName ?? "-"; } catch { }
            var expiryInfo = settings.LicenseExpiryDate.HasValue ? settings.LicenseExpiryDate.Value.ToString("yyyy-MM-dd") : "غير محدد";
            var activeToggle = settings.IsDeploymentActive ? "مُفعّل" : "مُعطّل";
            var warn = "";
            if (settings.LicenseExpiryDate.HasValue)
            {
                var days = (settings.LicenseExpiryDate.Value - DateTime.UtcNow).TotalDays;
                if (days < 0) warn = "<span style='color:#EF4444;font-weight:700'>منتهية!</span>";
                else if (days < 30) warn = $"<span style='color:#F59E0B;font-weight:700'>تنتهي خلال {Math.Ceiling(days)} يوم</span>";
            }
            var renewalDays = (settings.NextRenewalDate.Date - DateTime.UtcNow.Date).TotalDays;
            var renewalColor = renewalDays < 0 || renewalDays < 3 ? "#EF4444" : renewalDays < 14 ? "#F59E0B" : "#10B981";
            var renewalWarn = renewalDays < 0 ? $"<span style='color:#EF4444;font-weight:700'>متأخر {Math.Ceiling(-renewalDays)} يوم</span>" : $"<span style='color:{renewalColor};font-weight:700'>متبقي {Math.Ceiling(renewalDays)} يوم</span>";
            var renewalInfo = $"{settings.NextRenewalDate:yyyy-MM-dd} {renewalWarn} (دورة: {settings.SubscriptionCycle})";
            var cfg2 = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(cfg2);
            var statusBadge = settings.IsDeploymentActive ? "<span class='badge badge-on'>مُفعّل</span>" : "<span class='badge badge-off'>مُعطّل</span>";
            var licenseWarn = "";
            if (!string.IsNullOrWhiteSpace(warn)) licenseWarn = $"<div class='warn {(warn.Contains("منتهية") ? "warn-red" : "warn-amber")}'>{warn}</div>";
            var renewalWarnBox = "";
            if (renewalColor == "#EF4444") renewalWarnBox = $"<div class='warn warn-red'>{renewalInfo}</div>";
            else if (renewalColor == "#F59E0B") renewalWarnBox = $"<div class='warn warn-amber'>{renewalInfo}</div>";
            var body = $@"{licenseWarn}{renewalWarnBox}
<div class='stat-grid'>
<div class='stat'><b>{empCount}</b><span>الموظفين</span></div>
<div class='stat'><b>{invCount}</b><span>الفواتير</span></div>
<div class='stat'><b>{expiryInfo}</b><span>انتهاء الترخيص</span></div>
<div class='stat'><b>{settings.NextRenewalDate:yyyy-MM-dd}</b><span>التجديد القادم</span></div>
</div>
<div class='card'>
<h2>معلومات النشر</h2>
<table>
<tr><td>العميل المرخص له</td><td><b>{System.Net.WebUtility.HtmlEncode(settings.LicensedToClientName)}</b></td></tr>
<tr><td>الترخيص</td><td>{expiryInfo}</td></tr>
<tr><td>الحالة</td><td>{statusBadge} <form method='post' action='/{sec2}/toggle-active' style='display:inline;margin-inline-start:12px'><button class='ghost' type='submit'>تبديل</button></form></td></tr>
<tr><td>آخر مستخدم</td><td>{System.Net.WebUtility.HtmlEncode(lastLogin)}</td></tr>
<tr><td>الدعم</td><td>{System.Net.WebUtility.HtmlEncode(settings.SupportContactInfo ?? "-")}</td></tr>
<tr><td>التجديد</td><td>{renewalInfo} <form method='post' action='/{sec2}/subscription/renew' style='display:inline;margin-inline-start:12px'><button class='primary' style='background:#10B981' type='submit'>تجديد الآن</button></form></td></tr>
</table></div>";
            var html = OwnerLayout.Wrap("الرئيسية", sec2, body, "dash");
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        }
        app.MapGet(prefix + "/", WriteDashboard).AllowAnonymous();

        app.MapPost(prefix + "/toggle-active", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            await svc.UpdateAsync(s => s.IsDeploymentActive = !s.IsDeploymentActive);
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/" + sec2 + "/");
        }).DisableAntiforgery();
    }
}
