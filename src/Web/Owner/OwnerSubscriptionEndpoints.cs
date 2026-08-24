using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;

namespace ERPSystem.Web.Owner;

public static class OwnerSubscriptionEndpoints
{
    public static void Map(WebApplication app)
    {
        var secret = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var prefix = "/" + secret;

        app.MapGet(prefix + "/subscription", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            var s = await svc.GetAsync();
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            var opts = string.Join("", Enum.GetNames<SubscriptionCycle>().Select(n => $"<option value='{n}' {(s.SubscriptionCycle.ToString()==n?"selected":"")}>{n}</option>"));
            var daysLeft = (s.NextRenewalDate.Date - DateTime.UtcNow.Date).TotalDays;
            var warnBox = daysLeft < 0 ? $"<div class='warn warn-red'>متأخر {Math.Ceiling(-daysLeft)} يوم</div>" : daysLeft < 14 ? $"<div class='warn warn-amber'>متبقي {Math.Ceiling(daysLeft)} يوم</div>" : "";
            var body = $@"{warnBox}<div class='card'><h2>إدارة الاشتراك</h2>
<div class='stat-grid'><div class='stat'><b>{s.NextRenewalDate:yyyy-MM-dd}</b><span>التجديد القادم</span></div><div class='stat'><b>{(s.LastRenewedAt?.ToString("yyyy-MM-dd")??"-")}</b><span>آخر تجديد</span></div><div class='stat'><b>{s.SubscriptionCycle}</b><span>الدورة</span></div></div>
<form method='post' action='/{sec2}/subscription/save' style='margin-top:16px'><label>الدورة</label><select name='cycle'>{opts}</select><label>تاريخ البداية</label><input type='date' name='start' value='{s.SubscriptionStartDate:yyyy-MM-dd}'><label>التجديد القادم</label><input type='date' name='next' value='{s.NextRenewalDate:yyyy-MM-dd}'><button class='primary' type='submit' style='margin-top:12px'>حفظ</button></form>
<form method='post' action='/{sec2}/subscription/renew' style='margin-top:12px'><button class='primary' style='background:#10B981' type='submit'>تجديد الآن (+ دورة كاملة)</button></form></div>";
            var html = OwnerLayout.Wrap("الاشتراك", sec2, body, "sub");
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        });

        app.MapPost(prefix + "/subscription/save", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var form = await ctx.Request.ReadFormAsync();
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            var cycleStr = form["cycle"].ToString();
            Enum.TryParse<SubscriptionCycle>(cycleStr, out var cycle);
            DateTime.TryParse(form["start"].ToString(), out var start);
            DateTime.TryParse(form["next"].ToString(), out var next);
            await svc.UpdateAsync(s => { s.SubscriptionCycle = cycle; if (start != default) s.SubscriptionStartDate = start; if (next != default) s.NextRenewalDate = next; });
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/" + sec2 + "/subscription");
        }).DisableAntiforgery();

        app.MapPost(prefix + "/subscription/renew", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            await svc.UpdateAsync(s => { s.LastRenewedAt = DateTime.UtcNow; s.NextRenewalDate = SystemSettings.ComputeNextRenewal(s.NextRenewalDate > DateTime.UtcNow.Date ? s.NextRenewalDate : DateTime.UtcNow.Date, s.SubscriptionCycle); });
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            var referer = ctx.Request.Headers.Referer.ToString();
            ctx.Response.Redirect(string.IsNullOrWhiteSpace(referer) ? "/" + sec2 + "/" : referer.Contains("/subscription") ? "/" + sec2 + "/subscription" : "/" + sec2 + "/");
        }).DisableAntiforgery();
    }
}
