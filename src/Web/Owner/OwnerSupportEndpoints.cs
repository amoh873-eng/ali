using ERPSystem.Application.Interfaces;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;

namespace ERPSystem.Web.Owner;

public static class OwnerSupportEndpoints
{
    public static void Map(WebApplication app)
    {
        var secret = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var prefix = "/" + secret;

        app.MapGet(prefix + "/support", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; await ctx.Response.WriteAsync("Not Found"); return; }
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            var s = await svc.GetAsync();
            var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(cfg);
            var lic = string.IsNullOrWhiteSpace(s.LicenseExpiryDate?.ToString()) ? "" : s.LicenseExpiryDate!.Value.ToString("yyyy-MM-dd");
            var body = $@"<div class='card'><h2>معلومات الدعم والترخيص</h2>
<form method='post' action='/{sec2}/support/save'>
<label>العميل المرخص له</label><input name='LicensedTo' value='{System.Net.WebUtility.HtmlEncode(s.LicensedToClientName)}'>
<label>تاريخ انتهاء الترخيص</label><input name='Expiry' type='date' value='{lic}'>
<label>معلومات التواصل مع المورّد</label><textarea name='SupportContactInfo' rows='3'>{System.Net.WebUtility.HtmlEncode(s.SupportContactInfo ?? "")}</textarea>
<button class='primary' type='submit'>حفظ</button></form></div>";
            var html = OwnerLayout.Wrap("الدعم", sec2, body, "sup");
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        });

        app.MapPost(prefix + "/support/save", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var form = await ctx.Request.ReadFormAsync();
            var licensed = form["LicensedTo"].ToString();
            var expiryStr = form["Expiry"].ToString();
            var contact = form["SupportContactInfo"].ToString();
            DateTime? expiry = null;
            if (DateTime.TryParse(expiryStr, out var d)) expiry = d;
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            await svc.UpdateAsync(s =>
            {
                if (!string.IsNullOrWhiteSpace(licensed)) s.LicensedToClientName = licensed.Trim();
                s.LicenseExpiryDate = expiry;
                s.SupportContactInfo = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim();
            });
            var sec2b = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/" + sec2b + "/support");
        }).DisableAntiforgery();
    }
}
