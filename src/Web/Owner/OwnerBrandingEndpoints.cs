using ERPSystem.Application.Interfaces;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;

namespace ERPSystem.Web.Owner;

public static class OwnerBrandingEndpoints
{
    public static void Map(WebApplication app)
    {
        var secret = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var prefix = "/" + secret;

        app.MapGet(prefix + "/branding", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; await ctx.Response.WriteAsync("Not Found"); return; }
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            var s = await svc.GetAsync();
            var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(cfg);
            // ── قائمة العملات للعرض (الرمز ثابت، الاسم مترجم عبر resx) ──
            var curOpts = string.Join("", ERPSystem.Domain.CurrencyLookup.Symbols.Select(kv => $"<option value='{kv.Key}' {(s.CurrencyCode==kv.Key?"selected":"")}>{kv.Key} — {kv.Value}</option>"));
            var body = $@"<div class='card'><h2>الهوية البصرية والعملة</h2>
<div style='margin-bottom:16px'>{(!string.IsNullOrEmpty(s.LogoUrl) ? $"<img src='{s.LogoUrl}' style='max-width:180px;border-radius:10px;border:1px solid #E5E7EB;padding:8px;background:#fff'>" : "<span style='color:#9CA3AF'>لا يوجد شعار</span>")}</div>
<form method='post' action='/{sec2}/branding/save' enctype='multipart/form-data'>
<label>اسم النظام المعروض</label><input name='AppDisplayName' value='{System.Net.WebUtility.HtmlEncode(s.AppDisplayName ?? "")}' placeholder='اتركه فارغًا للافتراضي'>
<label>لون أساسي (hex)</label><input name='PrimaryColorHex' value='{System.Net.WebUtility.HtmlEncode(s.PrimaryColorHex ?? "")}' placeholder='#6D5BD0'>
<label>العملة — تغييرها يغير العرض فقط (لا يعيد حساب الأرقام)</label><select name='CurrencyCode'>{curOpts}</select>
<p style='font-size:.78rem;color:#6B7280;margin:4px 0 8px'>اختر عملة واحدة للنظام كله — كل المبالغ ستُعرض بهذا الرمز. التغيير عرض فقط.</p>
<label>رفع شعار جديد (png/jpg/svg, حتى 2MB)</label><input type='file' name='Logo' accept='image/*'>
<button class='primary' type='submit' style='margin-top:12px'>حفظ</button></form></div>";
            var html = OwnerLayout.Wrap("الهوية", sec2, body, "brand");
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        });

        app.MapPost(prefix + "/branding/save", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var form = await ctx.Request.ReadFormAsync();
            var displayName = form["AppDisplayName"].ToString();
            var color = form["PrimaryColorHex"].ToString();
            string? logoUrl = null;
            var file = form.Files["Logo"];
            if (file != null && file.Length > 0)
            {
                if (file.Length > 2 * 1024 * 1024) { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsync("File too large (max 2MB)"); return; }
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".svg" && ext != ".webp" && ext != ".gif")
                { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsync("Invalid image type"); return; }
                var webRoot = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath ?? "wwwroot";
                var uploads = Path.Combine(webRoot, "uploads", "branding");
                Directory.CreateDirectory(uploads);
                var fname = "logo" + ext;
                var fpath = Path.Combine(uploads, fname);
                using var fs = new FileStream(fpath, FileMode.Create);
                await file.CopyToAsync(fs);
                logoUrl = "/uploads/branding/" + fname;
            }
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            // ── حفظ العملة — عملية عرض فقط، لا تحويل مالي ──
            var cur = form["CurrencyCode"].ToString()?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(cur) || !ERPSystem.Domain.CurrencyLookup.Symbols.ContainsKey(cur)) cur = "JOD";
            await svc.UpdateAsync(s =>
            {
                s.AppDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
                if (!string.IsNullOrWhiteSpace(color)) s.PrimaryColorHex = color.Trim();
                else if (form.ContainsKey("PrimaryColorHex")) s.PrimaryColorHex = null;
                if (logoUrl != null) s.LogoUrl = logoUrl;
                s.CurrencyCode = cur!; // عرض فقط — نفس الأرقام، رمز مختلف
            });
            var sec2b = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/" + sec2b + "/branding");
        }).DisableAntiforgery();
    }
}
