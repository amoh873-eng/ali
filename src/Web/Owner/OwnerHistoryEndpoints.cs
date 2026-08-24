using ERPSystem.Infrastructure.Data;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Web.Owner;

public static class OwnerHistoryEndpoints
{
    public static void Map(WebApplication app)
    {
        var s = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var p = "/" + s;
        app.MapGet(p + "/history", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            using var scope = ctx.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var list = await db.SystemSettingsHistory.OrderByDescending(x => x.ChangedAt).Take(100).ToListAsync();
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            var rows = string.Join("", list.Select(h => $"<tr><td style='white-space:nowrap'>{h.ChangedAt:yyyy-MM-dd HH:mm}</td><td>{System.Net.WebUtility.HtmlEncode(h.ChangedBy ?? "-")}</td><td><pre style='max-width:520px;overflow:auto;font-size:.72rem;background:#F9FAFB;padding:8px;border-radius:8px;max-height:120px'>{System.Net.WebUtility.HtmlEncode(h.SnapshotJson.Length > 1200 ? h.SnapshotJson[..1200] + "..." : h.SnapshotJson)}</pre></td><td><form method='post' action='/{sec2}/history/restore/{h.Id}'><button class='ghost'>استعادة</button></form></td></tr>"));
            var body = $"<div class='card'><h2>سجل النسخ</h2><p style='color:#6B7280;font-size:.85rem'>كل تعديل يحفظ نسخة سابقة — يمكنك استعادتها.</p><table><tr><th>التاريخ</th><th>بواسطة</th><th>اللقطة</th><th>إجراء</th></tr>{(string.IsNullOrWhiteSpace(rows) ? "<tr><td colspan='4' style='text-align:center;color:#9CA3AF;padding:20px'>لا يوجد سجل بعد</td></tr>" : rows)}</table></div>";
            var html = OwnerLayout.Wrap("السجل", sec2, body, "hist");
            ctx.Response.ContentType = "text/html; charset=utf-8"; await ctx.Response.WriteAsync(html);
        });
        app.MapPost(p + "/history/restore/{id:guid}", async (HttpContext ctx, Guid id) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            using var scope = ctx.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var h = await db.SystemSettingsHistory.FirstOrDefaultAsync(x => x.Id == id);
            if (h == null) { ctx.Response.StatusCode = 404; return; }
            var current = await db.SystemSettings.FirstOrDefaultAsync();
            if (current != null)
            {
                try
                {
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(h.SnapshotJson);
                    if (dict != null)
                    {
                        var json = h.SnapshotJson;
                        var restored = System.Text.Json.JsonSerializer.Deserialize<ERPSystem.Domain.Entities.SystemSettings>(json);
                        if (restored != null)
                        {
                            db.Entry(current).CurrentValues.SetValues(restored);
                            current.Id = ERPSystem.Domain.Entities.SystemSettings.SingletonId;
                            await db.SaveChangesAsync();
                        }
                    }
                }
                catch { }
            }
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/" + sec2 + "/history");
        }).DisableAntiforgery();
    }
}
