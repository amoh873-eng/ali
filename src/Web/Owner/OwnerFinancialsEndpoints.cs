using ERPSystem.Infrastructure.Data;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
namespace ERPSystem.Web.Owner;
public static class OwnerFinancialsEndpoints
{
    public static void Map(WebApplication app)
    {
        var s = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var p = "/" + s;
        app.MapGet(p + "/financials", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            var f = ctx.Request.Query["from"].ToString(); var t = ctx.Request.Query["to"].ToString();
            DateTime.TryParse(f, out var from); DateTime.TryParse(t, out var to);
            if (from == default) from = DateTime.UtcNow.Date.AddDays(-30);
            if (to == default) to = DateTime.UtcNow.Date;
            using var scope = ctx.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var fromD = from.Date; var toD = to.Date.AddDays(1);
            var sales = await db.SalesInvoices.Where(x => !x.IsDeleted && x.InvoiceDate >= fromD && x.InvoiceDate < toD).SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
            var purch = await db.PurchaseInvoices.Where(x => !x.IsDeleted && x.InvoiceDate >= fromD && x.InvoiceDate < toD).SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
            decimal pay = 0; try { pay = await db.PayrollRunLines.Where(x => !x.IsDeleted && x.CreatedAt >= fromD && x.CreatedAt < toD).SumAsync(x => (decimal?)x.NetPay) ?? 0; if (pay == 0) pay = await db.PayrollRuns.Where(x => !x.IsDeleted && x.CreatedAt >= fromD && x.CreatedAt < toD).SumAsync(x => (decimal?)x.TotalNet) ?? 0; } catch {}
            decimal exp = 0; try { exp = await db.ExpenseEntries.Where(x => !x.IsDeleted && x.ExpenseDate >= fromD && x.ExpenseDate < toD).SumAsync(x => (decimal?)x.Amount) ?? 0; } catch {}
            var net = sales - purch - pay - exp;
            var fs = from.ToString("yyyy-MM-dd"); var ts = to.ToString("yyyy-MM-dd");
            var body = "<div class='card'><h2>التقرير المالي الشامل</h2><form method='get' action='/" + sec2 + "/financials' style='display:flex;gap:8px;flex-wrap:wrap;align-items:end;margin-bottom:12px'><label>من <input type='date' name='from' value='" + fs + "'></label><label>إلى <input type='date' name='to' value='" + ts + "'></label><button class='primary' type='submit'>عرض</button><a class='btn' href='/" + sec2 + "/financials/export?from=" + fs + "&to=" + ts + "' style='background:#10B981'>تصدير Excel</a></form><div class='stat-grid'><div class='stat'><b>" + sales.ToString("N2") + "</b><span>المبيعات</span></div><div class='stat'><b>" + purch.ToString("N2") + "</b><span>المشتريات</span></div><div class='stat'><b>" + pay.ToString("N2") + "</b><span>الرواتب</span></div><div class='stat'><b>" + exp.ToString("N2") + "</b><span>المصاريف</span></div><div class='stat' style='border:2px solid " + (net>=0?"#10B981":"#EF4444") + "'><b style='color:" + (net>=0?"#10B981":"#EF4444") + "'>" + net.ToString("N2") + "</b><span>الصافي</span></div></div><p style='color:#6B7280;font-size:.85rem'>الفترة: " + fs + " إلى " + ts + "</p></div>";
            var html = OwnerLayout.Wrap("التقرير المالي", sec2, body, "financials");
            ctx.Response.ContentType = "text/html; charset=utf-8"; await ctx.Response.WriteAsync(html);
        }).AllowAnonymous();
        app.MapGet(p + "/financials/export", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            var f = ctx.Request.Query["from"].ToString(); var t = ctx.Request.Query["to"].ToString();
            DateTime.TryParse(f, out var from); DateTime.TryParse(t, out var to);
            if (from == default) from = DateTime.UtcNow.Date.AddDays(-30);
            if (to == default) to = DateTime.UtcNow.Date;
            using var scope = ctx.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var fromD = from.Date; var toD = to.Date.AddDays(1);
            var sales = await db.SalesInvoices.Where(x => !x.IsDeleted && x.InvoiceDate >= fromD && x.InvoiceDate < toD).SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
            var purch = await db.PurchaseInvoices.Where(x => !x.IsDeleted && x.InvoiceDate >= fromD && x.InvoiceDate < toD).SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
            decimal pay2 = 0; try { pay2 = await db.PayrollRunLines.Where(x => !x.IsDeleted && x.CreatedAt >= fromD && x.CreatedAt < toD).SumAsync(x => (decimal?)x.NetPay) ?? 0; if (pay2 == 0) pay2 = await db.PayrollRuns.Where(x => !x.IsDeleted && x.CreatedAt >= fromD && x.CreatedAt < toD).SumAsync(x => (decimal?)x.TotalNet) ?? 0; } catch {}
            decimal exp2 = 0; try { exp2 = await db.ExpenseEntries.Where(x => !x.IsDeleted && x.ExpenseDate >= fromD && x.ExpenseDate < toD).SumAsync(x => (decimal?)x.Amount) ?? 0; } catch {}
            var net2 = sales - purch - pay2 - exp2;
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Financials");
            ws.Cell(1, 1).Value = "التقرير " + from.ToString("yyyy-MM-dd") + " - " + to.ToString("yyyy-MM-dd"); ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Value = "البند"; ws.Cell(3, 2).Value = "المبلغ"; ws.Row(3).Style.Font.Bold = true;
            ws.Cell(4, 1).Value = "المبيعات"; ws.Cell(4, 2).Value = sales;
            ws.Cell(5, 1).Value = "المشتريات"; ws.Cell(5, 2).Value = purch;
            ws.Cell(6, 1).Value = "الرواتب"; ws.Cell(6, 2).Value = pay2;
            ws.Cell(7, 1).Value = "المصاريف"; ws.Cell(7, 2).Value = exp2;
            ws.Cell(8, 1).Value = "الصافي"; ws.Cell(8, 2).Value = net2; ws.Row(8).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream(); wb.SaveAs(ms);
            ctx.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            ctx.Response.Headers["Content-Disposition"] = "attachment; filename=financials-" + from.ToString("yyyyMMdd") + "-" + to.ToString("yyyyMMdd") + ".xlsx";
            await ctx.Response.Body.WriteAsync(ms.ToArray(), ctx.RequestAborted);
        }).AllowAnonymous();
    }
}
