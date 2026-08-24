using ERPSystem.Infrastructure.Data;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Web.Owner;

public static class OwnerUpdateEndpoints
{
    public static void Map(WebApplication app)
    {
        var secret = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var prefix = "/" + secret;
        app.MapGet(prefix + "/updates", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            using var scope = ctx.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logs = await db.UpdateLogs.OrderByDescending(x=>x.CreatedAt).Take(50).ToListAsync(ctx.RequestAborted);
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            var updSvc = scope.ServiceProvider.GetService<ERPSystem.Infrastructure.Services.UpdateService>();
            string checkMsg=""; if(updSvc!=null){ var (ok,msg)=await updSvc.CheckForUpdateAsync(ctx.RequestAborted); checkMsg=$"<p style='color:#6B7280'>{System.Net.WebUtility.HtmlEncode(msg)}</p>"; }
            var rows = string.Join("", logs.Select(l=>$"<tr><td>{l.CreatedAt:yyyy-MM-dd HH:mm}</td><td>{System.Net.WebUtility.HtmlEncode(l.Version)}</td><td>{(l.Success?"<span class='badge badge-on'>OK</span>":"<span class='badge badge-off'>FAIL</span>")}</td><td style='font-size:.8rem'>{System.Net.WebUtility.HtmlEncode(l.Message??"-")}</td><td>{System.Net.WebUtility.HtmlEncode(l.TriggeredBy??"-")}</td></tr>"));
            var body=$@"<div class='card'><h2>التحديثات</h2>{checkMsg}
<form method='post' action='/{sec2}/updates/check' style='margin-bottom:12px'><button class='primary' type='submit'>فحص التحديثات</button></form>
<form method='post' action='/{sec2}/updates/apply' style='display:flex;gap:8px;margin-bottom:12px;flex-wrap:wrap'>
<input name='version' placeholder='version' required style='flex:1;min-width:120px'><input name='packagePath' placeholder='packagePath' required style='flex:1;min-width:200px'><input name='sha256' placeholder='SHA256' required style='flex:1;min-width:200px'><button class='primary' type='submit'>تطبيق</button></form>
<p style='font-size:.82rem;color:#92400E;background:#FEF3C7;padding:8px;border-radius:8px'>⚠️ قبل التطبيق: سيتم التحقق من الـ checksum وأخذ نسخة احتياطية تلقائيًا ثم تشغيل الفحوصات. أي فشل يُعرض بوضوح — الاستعادة من النسخة متاحة.</p></div>
<div class='card'><h2>سجل التحديثات</h2><table><tr><th>التاريخ</th><th>الإصدار</th><th>النتيجة</th><th>رسالة</th><th>بواسطة</th></tr>{(string.IsNullOrWhiteSpace(rows)?"<tr><td colspan='5' style='text-align:center;color:#9CA3AF'>لا يوجد سجل</td></tr>":rows)}</table></div>";
            var html=OwnerLayout.Wrap("التحديثات", sec2, body, "updates");
            ctx.Response.ContentType="text/html; charset=utf-8"; await ctx.Response.WriteAsync(html);
        }).AllowAnonymous();

        app.MapPost(prefix + "/updates/check", async (HttpContext ctx) =>
        {
            var ar=await ctx.AuthenticateAsync("OwnerScheme"); if(ar.Succeeded!=true){ctx.Response.StatusCode=404;return;}
            var sec2=OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/"+sec2+"/updates");
        }).DisableAntiforgery();
        app.MapPost(prefix + "/updates/apply", async (HttpContext ctx) =>
        {
            var ar=await ctx.AuthenticateAsync("OwnerScheme"); if(ar.Succeeded!=true){ctx.Response.StatusCode=404;return;}
            var form=await ctx.Request.ReadFormAsync();
            var version=form["version"].ToString(); var path=form["packagePath"].ToString(); var sha=form["sha256"].ToString();
            using var scope=ctx.RequestServices.CreateScope();
            var upd=scope.ServiceProvider.GetService<ERPSystem.Infrastructure.Services.UpdateService>();
            var user=ctx.User.Identity?.Name??"owner";
            if(upd!=null) await upd.ApplyUpdateAsync(version, path, sha, user, ctx.RequestAborted);
            var sec2=OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/"+sec2+"/updates");
        }).DisableAntiforgery();
    }
}
