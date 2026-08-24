using ERPSystem.Application.Interfaces;
using ERPSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication;

namespace ERPSystem.Web.Owner;

public static class OwnerTrialEndpoints
{
    public static void Map(WebApplication app)
    {
        var secret = OwnerMiddlewareHelpers.GetOwnerSecretPath(app.Configuration);
        var prefix = "/" + secret;
        app.MapGet(prefix + "/trial", async (HttpContext ctx) =>
        {
            var ar = await ctx.AuthenticateAsync("OwnerScheme");
            if (ar.Succeeded != true) { ctx.Response.StatusCode = 404; return; }
            using var scope = ctx.RequestServices.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            var s = await svc.GetAsync(ctx.RequestAborted);
            var sec2 = OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            var body=$@"<div class='card'><h2>الوضع التجريبي (3 أيام)</h2>
<p style='color:#6B7280'>الحالة: {(s.IsTrialMode? $"<span class='badge badge-on'>مفعل — ينتهي {s.TrialExpiresAt:yyyy-MM-dd HH:mm}</span>" : "<span class='badge badge-off'>غير مفعل</span>")}</p>
<form method='post' action='/{sec2}/trial/activate'><button class='primary' type='submit'>تفعيل تجريبي (3 أيام)</button></form>
<form method='post' action='/{sec2}/trial/deactivate' style='margin-top:8px'><button class='ghost' type='submit'>إلغاء التجريبي</button></form>
<p style='font-size:.82rem;color:#6B7280'>عند انتهاء 3 أيام يُقفل النظام تلقائيًا برسالة تجريبية + بيانات التواصل.</p>
<hr style='margin:16px 0'>
<h3>بيانات تجريبية</h3><p style='font-size:.85rem;color:#6B7280'>تعبئة زبائن/أصناف/فواتير/موظفين وهمية للعرض التسويقي</p>
<form method='post' action='/{sec2}/trial/seed'><button class='primary' type='submit'>تعبئة بيانات وهمية</button></form>
</div>";
            var html=OwnerLayout.Wrap("التجريبي", sec2, body, "trial");
            ctx.Response.ContentType="text/html; charset=utf-8"; await ctx.Response.WriteAsync(html);
        }).AllowAnonymous();

        app.MapPost(prefix + "/trial/activate", async (HttpContext ctx) =>
        {
            var ar=await ctx.AuthenticateAsync("OwnerScheme"); if(ar.Succeeded!=true){ctx.Response.StatusCode=404;return;}
            using var scope=ctx.RequestServices.CreateScope();
            var svc=scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            await svc.UpdateAsync(s=>{ s.IsTrialMode=true; s.TrialStartedAt=DateTime.UtcNow; s.TrialExpiresAt=DateTime.UtcNow.AddDays(3); s.IsDeploymentActive=true; }, ctx.RequestAborted);
            var sec2=OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/"+sec2+"/trial");
        }).DisableAntiforgery();
        app.MapPost(prefix + "/trial/deactivate", async (HttpContext ctx) =>
        {
            var ar=await ctx.AuthenticateAsync("OwnerScheme"); if(ar.Succeeded!=true){ctx.Response.StatusCode=404;return;}
            using var scope=ctx.RequestServices.CreateScope();
            var svc=scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            await svc.UpdateAsync(s=>{ s.IsTrialMode=false; s.TrialStartedAt=null; s.TrialExpiresAt=null; }, ctx.RequestAborted);
            var sec2=OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/"+sec2+"/trial");
        }).DisableAntiforgery();
        app.MapPost(prefix + "/trial/seed", async (HttpContext ctx) =>
        {
            var ar=await ctx.AuthenticateAsync("OwnerScheme"); if(ar.Succeeded!=true){ctx.Response.StatusCode=404;return;}
            using var scope=ctx.RequestServices.CreateScope();
            var sp=scope.ServiceProvider;
            await SeedDemoDataAsync(sp);
            var sec2=OwnerMiddlewareHelpers.GetOwnerSecretPath(ctx.RequestServices.GetRequiredService<IConfiguration>());
            ctx.Response.Redirect("/"+sec2+"/trial");
        }).DisableAntiforgery();
    }

    private static async Task SeedDemoDataAsync(IServiceProvider sp)
    {
        var db=sp.GetRequiredService<ERPSystem.Infrastructure.Data.AppDbContext>();
        // Seed realistic QA dataset: customers, items with movements, suppliers, employees, invoices, etc.
        var wh=db.Warehouses.FirstOrDefault();
        if(wh==null) return;
        var cat=db.Categories.FirstOrDefault();
        var unit=db.Units.FirstOrDefault();
        // Customers
        for(int i=6;i<=8;i++){
            if(db.Customers.Any(c=>c.Code==$"QA-CUST{i}")) continue;
            db.Customers.Add(new ERPSystem.Domain.Entities.Customer{ Id=Guid.NewGuid(), Code=$"QA-CUST{i}", NameAr=$"عميل فحص {i}", NameEn=$"QA Customer {i}", Phone=$"96279{i:D7}", IsActive=true, CreatedAt=DateTime.UtcNow });
        }
        // Items
        for(int i=6;i<=10;i++){
            if(db.Items.Any(it=>it.Code==$"QA-ITEM{i}")) continue;
            var it=new ERPSystem.Domain.Entities.Item{ Id=Guid.NewGuid(), Code=$"QA-ITEM{i}", NameAr=$"صنف فحص {i}", NameEn=$"QA Item {i}", CategoryId=cat?.Id ?? Guid.Empty, UnitId=unit?.Id ?? Guid.Empty, CostPrice=10*i, SalePrice=15*i, CurrentStock=50+i*10, IsActive=true, CreatedAt=DateTime.UtcNow };
            if(unit!=null) it.UnitId=unit.Id;
            if(cat!=null) it.CategoryId=cat.Id;
            db.Items.Add(it);
        }
        db.SaveChanges();
        // Suppliers
        for(int i=1;i<=2;i++){
            if(db.Suppliers.Any(s=>s.Code==$"QA-SUP{i}")) continue;
            db.Suppliers.Add(new ERPSystem.Domain.Entities.Supplier{ Id=Guid.NewGuid(), Code=$"QA-SUP{i}", NameAr=$"مورد فحص {i}", NameEn=$"QA Supplier {i}", IsActive=true, CreatedAt=DateTime.UtcNow });
        }
        // Departments/Positions already seeded; add a test employee if missing
        var dept=db.Departments.FirstOrDefault();
        var pos=db.Positions.FirstOrDefault();
        if(!db.Employees.Any(e=>e.EmployeeNumber=="QA-EMP1") && dept!=null && pos!=null){
            db.Employees.Add(new ERPSystem.Domain.Entities.Employee{ Id=Guid.NewGuid(), EmployeeNumber="QA-EMP1", NameAr="موظف فحص", DepartmentId=dept.Id, PositionId=pos.Id, HireDate=DateTime.UtcNow.Date.AddMonths(-2), BasicSalary=800, CreatedAt=DateTime.UtcNow });
        }
        db.SaveChanges();
        // Seed a Sales Invoice via domain if none exists (use QA data)
        if(!db.SalesInvoices.Any()){
            var cust=db.Customers.FirstOrDefault(c=>c.Code.StartsWith("QA-CUST")) ?? db.Customers.FirstOrDefault();
            var item=db.Items.FirstOrDefault(it=>it.Code.StartsWith("QA-ITEM")) ?? db.Items.FirstOrDefault();
            if(cust!=null && item!=null){
                var inv=new ERPSystem.Domain.Entities.SalesInvoice{ Id=Guid.NewGuid(), InvoiceNumber=$"QA-SI-{DateTime.UtcNow:yyyyMMdd}-001", CustomerId=cust.Id, WarehouseId=wh.Id, InvoiceDate=DateTime.UtcNow.Date, InvoiceType=ERPSystem.Domain.Enums.SalesInvoiceType.Cash, Status=ERPSystem.Domain.Enums.DocumentStatus.Posted, SubTotal=item.SalePrice*2, DiscountPercentage=0, DiscountAmount=0, TaxRate=16, TaxAmount=Math.Round(item.SalePrice*2*0.16m,2), TotalAmount=Math.Round(item.SalePrice*2*1.16m,2), PaidAmount=Math.Round(item.SalePrice*2*1.16m,2), CreatedAt=DateTime.UtcNow };
                db.SalesInvoices.Add(inv);
                db.SalesInvoiceLines.Add(new ERPSystem.Domain.Entities.SalesInvoiceLine{ Id=Guid.NewGuid(), SalesInvoiceId=inv.Id, ItemId=item.Id, Quantity=2, UnitPrice=item.SalePrice, UnitCost=item.CostPrice, LineTotal=item.SalePrice*2, CreatedAt=DateTime.UtcNow });
                db.StockMovements.Add(new ERPSystem.Domain.Entities.StockMovement{ Id=Guid.NewGuid(), ItemId=item.Id, WarehouseId=wh.Id, MovementType=ERPSystem.Domain.Enums.MovementType.SalesIssue, Quantity=-2, UnitCost=item.CostPrice, ReferenceNumber=inv.InvoiceNumber, MovementDate=DateTime.UtcNow.Date, CreatedAt=DateTime.UtcNow });
                db.SaveChanges();
            }
        }
        await Task.CompletedTask;
    }
}
