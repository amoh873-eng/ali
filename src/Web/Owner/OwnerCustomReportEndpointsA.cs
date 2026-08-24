using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
namespace ERPSystem.Web.Owner;
public static class OwnerCustomReportEndpoints
{
    public static void Map(WebApplication app)
    {
        var s = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var p = "/" + s;
        app.MapGet(p + "/reports", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            using var scp = ctx.RequestServices.CreateScope();
            var svc = scp.ServiceProvider.GetRequiredService<ICustomReportService>();
            var list = await svc.GetAllAsync();
            var sec = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            var rows = string.Join("", list.Select(r => "<tr><td><b>" + System.Net.WebUtility.HtmlEncode(r.Name) + "</b></td><td>" + r.ReportType + "</td><td>" + (r.IsEnabled ? "On" : "Off") + "</td><td><a href='/" + sec + "/reports/edit/" + r.Id + "'>Edit</a> <form method='post' action='/" + sec + "/reports/delete/" + r.Id + "' style='display:inline'><button>Del</button></form></td></tr>"));
            var body = "<div class='card'><h2>Custom Reports</h2><a class='btn' href='/" + sec + "/reports/new'>+ New</a><table style='margin-top:14px'><tr><th>Name</th><th>Type</th><th>Status</th><th>Action</th></tr>" + (string.IsNullOrWhiteSpace(rows) ? "<tr><td colspan='4' style='text-align:center;color:#9CA3AF;padding:24px'>None</td></tr>" : rows) + "</table></div>";
            var html = OwnerLayout.Wrap("Reports", sec, body, "reports");
            ctx.Response.ContentType = "text/html; charset=utf-8"; await ctx.Response.WriteAsync(html);
        });
        app.MapGet(p + "/reports/new", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme"); if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var sec = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            var body = "<div class='card'><h2>New Report</h2><form method='post' action='/" + sec + "/reports/create' style='display:flex;flex-direction:column;gap:12px'><label>Name *</label><input name='Name' required><label>Description</label><textarea name='Description' rows='2'></textarea><label>Client Note</label><textarea name='ClientRequestNote' rows='2'></textarea><label>Type</label><select name='ReportType'><option value='SqlQuery'>SqlQuery</option><option value='ExistingReportVariant'>Variant</option></select><label>SQL</label><textarea name='SqlQuery' rows='6'></textarea><label>Key</label><input name='ExistingReportKey'><label>Params</label><textarea name='ParametersJson' rows='2'>[]</textarea><label><input type='checkbox' name='IsEnabled' value='true' checked> Enabled</label><button class='primary' type='submit'>Save</button></form></div>";
            var html = OwnerLayout.Wrap("New Report", sec, body, "reports");
            ctx.Response.ContentType = "text/html; charset=utf-8"; await ctx.Response.WriteAsync(html);
        });
    }
}
