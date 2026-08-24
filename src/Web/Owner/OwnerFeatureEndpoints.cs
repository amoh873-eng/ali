using ERPSystem.Application.Interfaces;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;

namespace ERPSystem.Web.Owner;

public static class OwnerFeatureEndpoints
{
    public static void Map(WebApplication app)
    {
        var secret = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var prefix = "/" + secret;

        app.MapGet(prefix + "/features", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; await ctx.Response.WriteAsync("Not Found"); return; }
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            var s = await svc.GetAsync();
            var flags = s.GetFeatureFlags();
            var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(cfg);
            string LabelFor(string k) => k switch { "Sales"=>"المبيعات","Purchases"=>"المشتريات","Inventory"=>"المخزون","Accounting"=>"المحاسبة","Expenses"=>"المصروفات","Crm"=>"علاقات العملاء","Hr"=>"الموارد البشرية","Reports"=>"التقارير","Pos"=>"نقطة البيع","Permissions"=>"الصلاحيات","JoFotara"=>"الفوترة الإلكترونية", _=>k };
            var rows = string.Join("", flags.Select(kv => $@"<label style='display:flex;align-items:center;gap:10px;padding:10px 12px;border:1px solid #E5E7EB;border-radius:10px;margin:6px 0;background:{(kv.Value?"#F0EDFB":"#fff")}'><input type='checkbox' name='flag_{kv.Key}' value='true' {(kv.Value?"checked":"")} style='width:18px;height:18px'> <b>{LabelFor(kv.Key)}</b> <span style='color:#6B7280;font-size:.85rem'>({kv.Key})</span></label>"));
            var body = $@"<div class='card'><h2>الموديولات المتاحة</h2><p style='color:#6B7280;font-size:.9rem'>أوقف الموديول لإخفائه من القائمة ومنع الوصول المباشر — حتى لو كان للمستخدم صلاحية الدور.</p><form method='post' action='/{sec2}/features/save'>{rows}<button class='primary' type='submit' style='margin-top:14px'>حفظ</button></form></div>";
            var html = OwnerLayout.Wrap("الموديولات", sec2, body, "feat");
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        });

        app.MapPost(prefix + "/features/save", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var form = await ctx.Request.ReadFormAsync();
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            var updated = new Dictionary<string,bool>();
            foreach (var k in ERPSystem.Domain.Entities.SystemSettings.AllModuleKeys)
                updated[k] = form.ContainsKey("flag_" + k) && form["flag_" + k].ToString() == "true";
            await svc.UpdateAsync(s => s.SetFeatureFlags(updated));
            var sec2b = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/" + sec2b + "/features");
        }).DisableAntiforgery();
    }
}
