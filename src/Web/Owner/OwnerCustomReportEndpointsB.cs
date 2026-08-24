using ERPSystem.Domain.Enums;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
namespace ERPSystem.Web.Owner;
public static class OwnerCustomReportEndpointsB
{
    public static void Map(WebApplication app)
    {
        var s = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var p = "/" + s;
        app.MapPost(p + "/reports/create", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme"); if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var form = await ctx.Request.ReadFormAsync();
            var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<ERPSystem.Application.Interfaces.ICustomReportService>();
            var rt = form["ReportType"].ToString() == "ExistingReportVariant" ? CustomReportType.ExistingReportVariant : CustomReportType.SqlQuery;
            var en = form["IsEnabled"].ToString() == "true";
            try { await svc.CreateAsync(new ERPSystem.Domain.Entities.CustomReportDefinition { Name = form["Name"].ToString(), Description = form["Description"].ToString(), ClientRequestNote = form["ClientRequestNote"].ToString(), ReportType = rt, SqlQuery = form["SqlQuery"].ToString(), ExistingReportKey = form["ExistingReportKey"].ToString(), ParametersJson = form["ParametersJson"].ToString(), IsEnabled = en }); var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()); ctx.Response.Redirect("/" + sec2 + "/reports"); } catch (Exception ex) { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsync(System.Net.WebUtility.HtmlEncode(ex.Message)); }
        }).DisableAntiforgery();
        app.MapPost(p + "/reports/delete/{id:guid}", async (HttpContext ctx, Guid id) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme"); if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<ERPSystem.Application.Interfaces.ICustomReportService>();
            await svc.DeleteAsync(id);
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());
            ctx.Response.Redirect("/" + sec2 + "/reports");
        }).DisableAntiforgery();
        app.MapGet(p + "/reports/edit/{id:guid}", async (HttpContext ctx, Guid id) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme"); if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<ERPSystem.Application.Interfaces.ICustomReportService>();
            var e = await svc.GetByIdAsync(id);
            if (e == null) { ctx.Response.StatusCode = 404; return; }
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());
            string H(string? v) => System.Net.WebUtility.HtmlEncode(v ?? "");
            var body = "<div class='card'><h2>Edit</h2><form method='post' action='/" + sec2 + "/reports/update/" + e.Id + "' style='display:flex;flex-direction:column;gap:10px'><label>Name</label><input name='Name' value='" + H(e.Name) + "' required><label>Description</label><textarea name='Description'>" + H(e.Description) + "</textarea><label>Client Note</label><textarea name='ClientRequestNote'>" + H(e.ClientRequestNote) + "</textarea><label>Type</label><select name='ReportType'><option value='SqlQuery'" + (e.ReportType == CustomReportType.SqlQuery ? " selected" : "") + ">SqlQuery</option><option value='ExistingReportVariant'" + (e.ReportType == CustomReportType.ExistingReportVariant ? " selected" : "") + ">Variant</option></select><label>SQL</label><textarea name='SqlQuery'>" + H(e.SqlQuery) + "</textarea><label>Key</label><input name='ExistingReportKey' value='" + H(e.ExistingReportKey) + "'><label>Params</label><textarea name='ParametersJson'>" + H(e.ParametersJson) + "</textarea><label><input type='checkbox' name='IsEnabled' value='true'" + (e.IsEnabled ? " checked" : "") + "> Enabled</label><button class='primary' type='submit'>Save</button></form></div>";
            var html = OwnerLayout.Wrap("Edit", sec2, body, "reports");
            ctx.Response.ContentType = "text/html; charset=utf-8"; await ctx.Response.WriteAsync(html);
        });
        app.MapPost(p + "/reports/update/{id:guid}", async (HttpContext ctx, Guid id) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme"); if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var form = await ctx.Request.ReadFormAsync();
            var rt = form["ReportType"].ToString() == "ExistingReportVariant" ? CustomReportType.ExistingReportVariant : CustomReportType.SqlQuery;
            var en = form["IsEnabled"].ToString() == "true";
            var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<ERPSystem.Application.Interfaces.ICustomReportService>();
            try { await svc.UpdateAsync(id, x => { x.Name = form["Name"].ToString(); x.Description = form["Description"].ToString(); x.ClientRequestNote = form["ClientRequestNote"].ToString(); x.ReportType = rt; x.SqlQuery = form["SqlQuery"].ToString(); x.ExistingReportKey = form["ExistingReportKey"].ToString(); x.ParametersJson = form["ParametersJson"].ToString(); x.IsEnabled = en; }); var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()); ctx.Response.Redirect("/" + sec2 + "/reports"); } catch (Exception ex) { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsync(System.Net.WebUtility.HtmlEncode(ex.Message)); }
        }).DisableAntiforgery();
    }
}
