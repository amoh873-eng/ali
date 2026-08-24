using System.Net;
using ERPSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace ERPSystem.Web.Middleware;

public static class OwnerMiddlewareHelpers
{
    public static string GetOwnerSecretPath(IConfiguration cfg) =>
        cfg["Owner:SecretPath"] ?? cfg["Owner__SecretPath"] ?? "x-vendor-9f3a1c";

    public static bool IsOwnerPath(HttpContext ctx, string secretPath)
    {
        var p = ctx.Request.Path.Value ?? "";
        return p.Equals("/" + secretPath, StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/" + secretPath + "/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsIpAllowed(HttpContext ctx, string? allowedIps)
    {
        if (string.IsNullOrWhiteSpace(allowedIps)) return true;
        var remoteIp = ctx.Connection.RemoteIpAddress;
        if (remoteIp == null) return false;
        if (remoteIp.IsIPv4MappedToIPv6) remoteIp = remoteIp.MapToIPv4();
        var entries = allowedIps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var entry in entries)
        {
            var e = entry.Trim();
            if (e.Contains('/'))
            {
                var parts = e.Split('/');
                if (parts.Length != 2) continue;
                if (!IPAddress.TryParse(parts[0], out var network)) continue;
                if (!int.TryParse(parts[1], out var prefixLen)) continue;
                if (network.IsIPv4MappedToIPv6) network = network.MapToIPv4();
                if (remoteIp.AddressFamily != network.AddressFamily) continue;
                var addrBytes = network.GetAddressBytes();
                var ipBytes = remoteIp.GetAddressBytes();
                int fullBytes = prefixLen / 8;
                int remBits = prefixLen % 8;
                bool match = true;
                for (int i = 0; i < fullBytes; i++) if (addrBytes[i] != ipBytes[i]) { match = false; break; }
                if (!match) continue;
                if (remBits > 0)
                {
                    int mask = 0xFF << (8 - remBits) & 0xFF;
                    if ((addrBytes[fullBytes] & mask) != (ipBytes[fullBytes] & mask)) continue;
                }
                return true;
            }
            else
            {
                if (!IPAddress.TryParse(e, out var allowed)) continue;
                if (allowed.IsIPv4MappedToIPv6) allowed = allowed.MapToIPv4();
                if (allowed.Equals(remoteIp)) return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Blocks non-owner requests when IsDeploymentActive=false. Never blocks owner paths.
/// </summary>
public class DeploymentActiveMiddleware
{
    private readonly RequestDelegate _next;
    public DeploymentActiveMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext ctx)
    {
        var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
        var secretPath = OwnerMiddlewareHelpers.GetOwnerSecretPath(cfg);
        if (OwnerMiddlewareHelpers.IsOwnerPath(ctx, secretPath))
        {
            await _next(ctx);
            return;
        }
        // Do not block static assets, framework endpoints, or auth pages — only normal app pages
        var path = ctx.Request.Path.Value ?? "";
        var lower = path.ToLowerInvariant();
        bool isExempt = lower.StartsWith("/_") || lower.StartsWith("/__") || lower.StartsWith("/_blazor") || lower.StartsWith("/_content") || lower.StartsWith("/_framework")
            || lower.StartsWith("/css") || lower.StartsWith("/js") || lower.StartsWith("/lib") || lower.StartsWith("/uploads") || lower.StartsWith("/favicon")
            || lower.Equals("/login") || lower.StartsWith("/login/") || lower.Equals("/logout") || lower.Equals("/access-denied") || lower.Equals("/not-found") || lower.Equals("/error");
        if (isExempt) { await _next(ctx); return; }
        // Also exempt static file extensions
        if (lower.EndsWith(".css") || lower.EndsWith(".js") || lower.EndsWith(".map") || lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".svg") || lower.EndsWith(".woff") || lower.EndsWith(".woff2") || lower.EndsWith(".ico"))
        { await _next(ctx); return; }
        try
        {
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            var settings = await svc.GetAsync();
            if (!settings.IsDeploymentActive)
            {
                ctx.Response.StatusCode = 503;
                ctx.Response.ContentType = "text/html; charset=utf-8";
                var contact = System.Net.WebUtility.HtmlEncode(settings.SupportContactInfo ?? "");
                var isTrial = settings.IsTrialMode && settings.TrialExpiresAt.HasValue && DateTime.UtcNow >= settings.TrialExpiresAt.Value;
                var title = isTrial ? "انتهت مدة العرض التجريبي — تواصل معنا لطلب نسخة كاملة" : "هذا النظام غير مُفعّل حاليًا، الرجاء التواصل مع المورّد";
                var html = $@"<!doctype html><html lang='ar' dir='rtl'><head><meta charset='utf-8'><title>النظام غير مُفعّل</title>
<style>body{{font-family:Cairo,Tajawal,sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;background:#F4F5F9;margin:0}}
.card{{background:#fff;padding:32px 28px;border-radius:12px;box-shadow:0 4px 24px rgba(0,0,0,.08);max-width:560px;text-align:center}}
h1{{color:#6D5BD0;margin:0 0 12px}}p{{color:#444;line-height:1.7}}small{{color:#888}}</style></head>
<body><div class='card'><h1>{title}</h1>
<p>{(string.IsNullOrWhiteSpace(contact) ? "" : $"<br>تواصل: {contact}")}</p></div></body></html>";
                await ctx.Response.WriteAsync(html);
                return;
            }
        }
        catch { }
        await _next(ctx);
    }
}
