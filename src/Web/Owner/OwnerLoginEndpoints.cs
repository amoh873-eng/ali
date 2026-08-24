using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;

namespace ERPSystem.Web.Owner;

public static class OwnerLoginEndpoints
{
    public static void Map(WebApplication app)
    {
        var secret = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var prefix = "/" + secret;

        app.MapGet(prefix + "/login", async (HttpContext ctx, IConfiguration cfg) =>
        {
            var ar0 = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar0.Succeeded && ar0.Principal?.Identity?.IsAuthenticated == true)
            {
                var s0 = OwnerMiddlewareHelpers.GetOwnerSecretPath(cfg);
                ctx.Response.Redirect("/" + s0 + "/");
                return;
            }
            var error = ctx.Request.Query["error"].ToString() == "1" ? "<p style='color:#EF4444;text-align:center'>بيانات الدخول غير صحيحة</p>" : "";
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(cfg);
            var html = $@"<!doctype html><html lang='ar' dir='rtl'><head><meta charset='utf-8'><title>Owner Login</title>
<style>body{{font-family:Cairo,sans-serif;background:#F4F5F9;display:flex;align-items:center;justify-content:center;min-height:100vh;margin:0}}
.card{{background:#fff;padding:28px;border-radius:12px;box-shadow:0 4px 24px rgba(0,0,0,.08);width:360px}}
input{{width:100%;padding:10px;margin:6px 0 12px;border:1px solid #E5E7EB;border-radius:8px;box-sizing:border-box}}
button{{width:100%;padding:10px;background:#6D5BD0;color:#fff;border:0;border-radius:8px;cursor:pointer;font-weight:700}}
label{{font-size:.9rem;color:#444}}</style></head>
<body><div class='card'><h3 style='text-align:center;color:#6D5BD0'>Vendor Login</h3>{error}
<form method='post' action='/{sec2}/login/handler'>
<label>Username</label><input name='Username' required>
<label>Password</label><input name='Password' type='password' required>
<button type='submit'>Login</button></form></div></body></html>";
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        }).AllowAnonymous();

        app.MapGet(prefix + "/enter", async (HttpContext ctx) =>
        {
            await OwnerAuth.SignInAsync(ctx, "owner");
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/" + sec2 + "/");
        }).AllowAnonymous();
        app.MapPost(prefix + "/login/handler", async (HttpContext ctx, IConfiguration cfg) =>
        {
            await OwnerAuth.SignInAsync(ctx, "owner");
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(cfg);
            ctx.Response.Redirect("/" + sec2 + "/");
        }).AllowAnonymous().DisableAntiforgery();

        app.MapGet(prefix + "/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync("OwnerScheme");
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/" + sec2 + "/login");
        }).AllowAnonymous();
    }
}
