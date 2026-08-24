using ERPSystem.Domain.Entities;
using ERPSystem.Infrastructure.Data;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ERPSystem.Web.Owner;

public static class OwnerHealthEndpoints
{
    public static void Map(WebApplication app)
    {
        var secret = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var prefix = "/" + secret;
        app.MapGet(prefix + "/health", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var health = ctx.RequestServices.GetRequiredService<HealthCheckService>();
            var report = await health.CheckHealthAsync(ctx.RequestAborted);
            using var scope = ctx.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var snapshots = await db.HealthSnapshots.OrderByDescending(x => x.CheckedAt).Take(7).ToListAsync(ctx.RequestAborted);
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            string Color(string s) => s == "Healthy" ? "#10B981" : s == "Degraded" ? "#F59E0B" : "#EF4444";
            string Badge(string s) => $"<span style='padding:4px 10px;border-radius:20px;font-size:.8rem;font-weight:700;background:{(s=="Healthy"?"#E8F5E9":s=="Degraded"?"#FEF3C7":"#FEE2E2")};color:{Color(s)}'>{s}</span>";
            var rows = string.Join("", report.Entries.Select(kv => $"<tr><td><b>{kv.Key}</b></td><td>{Badge(kv.Value.Status.ToString())}</td><td style='font-size:.85rem'>{System.Net.WebUtility.HtmlEncode(kv.Value.Description ?? "-")}</td><td style='font-size:.82rem;color:#6B7280'>{kv.Value.Duration.TotalMilliseconds:F0}ms</td></tr>"));
            var hist = string.Join("", snapshots.Select(s => $"<tr><td>{s.CheckedAt:yyyy-MM-dd HH:mm}</td><td>{Badge(s.OverallStatus)}</td><td style='font-size:.75rem;max-width:400px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>{System.Net.WebUtility.HtmlEncode(s.ResultsJson.Length>300?s.ResultsJson[..300]+"...":s.ResultsJson)}</td></tr>"));
            var overall = Badge(report.Status.ToString());
            var body = $@"<div class='card'><h2>صحة النظام — {overall} <form method='post' action='/{sec2}/health/recheck' style='display:inline;margin-inline-start:12px'><button class='primary' type='submit'>إعادة الفحص</button></form></h2>
<table><tr><th>الفحص</th><th>الحالة</th><th>التفاصيل</th><th>الزمن</th></tr>{rows}</table>
<p style='color:#6B7280;font-size:.85rem'>آخر فحص: {DateTime.UtcNow:u} — الإجمالي: {report.TotalDuration.TotalMilliseconds:F0}ms</p></div>
<div class='card'><h2>آخر 7 لقطات</h2><table><tr><th>التاريخ</th><th>الحالة</th><th>النتيجة</th></tr>{(string.IsNullOrWhiteSpace(hist)?"<tr><td colspan='3' style='text-align:center;color:#9CA3AF'>لا توجد لقطات بعد</td></tr>":hist)}</table></div>";
            var html = OwnerLayout.Wrap("الصحة", sec2, body, "health");
            ctx.Response.ContentType = "text/html; charset=utf-8"; await ctx.Response.WriteAsync(html);
        }).AllowAnonymous();

        app.MapPost(prefix + "/health/recheck", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var health = ctx.RequestServices.GetRequiredService<HealthCheckService>();
            var report = await health.CheckHealthAsync(ctx.RequestAborted);
            using var scope = ctx.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var json = System.Text.Json.JsonSerializer.Serialize(report.Entries.ToDictionary(kv=>kv.Key, kv=>new{ status=kv.Value.Status.ToString(), desc=kv.Value.Description }));
            db.HealthSnapshots.Add(new HealthSnapshot{ CheckedAt=DateTime.UtcNow, OverallStatus=report.Status.ToString(), ResultsJson=json });
            await db.SaveChangesAsync(ctx.RequestAborted);
            try{
                var notifier = scope.ServiceProvider.GetService<ERPSystem.Infrastructure.Services.ErrorNotifierService>();
                foreach(var kv in report.Entries.Where(kv=>kv.Value.Status!=HealthStatus.Healthy))
                    if(notifier!=null) await notifier.NotifyHealthChangeAsync(kv.Key, kv.Value.Status.ToString(), kv.Value.Description, ctx.RequestAborted);
            }catch{}
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/"+sec2+"/health");
        }).DisableAntiforgery();

        // Anonymous ping for uptime monitors
        app.MapGet("/health/ping", () => Results.Ok("pong")).AllowAnonymous();
    }
}
