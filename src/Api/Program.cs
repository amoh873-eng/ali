using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ERPSystem.Infrastructure;
using ERPSystem.Infrastructure.Data;
using ERPSystem.Application.Interfaces;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Services;
using ERPSystem.Api.Security;
using ERPSystem.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// خدمات يثبّتها برنامج Web صراحةً — إعادة تسجيل المطلوب هنا (حل DI لخدمات AddInfrastructure)
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<ErrorNotifierService>();
builder.Services.AddScoped<UpdateService>();
builder.Services.AddScoped<IRepCustodyService, ERPSystem.Application.Services.RepCustodyService>();
builder.Services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(p => p.GetRequiredService<ERPSystem.Infrastructure.Data.AppDbContext>());
builder.Services.AddScoped<IShelfService, ERPSystem.Application.Services.ShelfService>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 10;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

// جدول مفاتيح التكرار (Idempotency) داخل الخادم: clientRequestId → invoiceId
var _idem = new Dictionary<string, string>();

// ── POST /api/auth/login (عام — يكتب JSON يدوياً لأن معالجات POST لا تعيد جسماً) ──
app.MapPost("/api/auth/login", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    var signInManager = ctx.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
    var userManager = ctx.RequestServices.GetRequiredService<UserManager<IdentityUser>>();

    using var buffer = new MemoryStream();
    await ctx.Request.Body.CopyToAsync(buffer);
    buffer.Position = 0;
    using var rd = new StreamReader(buffer);
    var sbJson = new StringBuilder();
    string? line;
    while ((line = rd.ReadLine()) is not null) sbJson.Append(line);
    System.Text.Json.JsonElement root;
    try { root = System.Text.Json.JsonDocument.Parse(sbJson.ToString()).RootElement; }
    catch { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }

    var loginName = root.TryGetProperty("email", out var emailNode) ? (emailNode.GetString() ?? "").Trim() : "";
    var password = root.TryGetProperty("password", out var passNode) ? passNode.GetString() ?? "" : "";
    if (string.IsNullOrEmpty(loginName) || string.IsNullOrEmpty(password)) { await WriteJson(ctx, 400, "{\"error\":\"Email and password required\"}"); return; }

    // الدخول باسم المستخدم أو البريد معاً (Identity يُطابق اسم المستخدم داخلياً)
    var user = await userManager.FindByNameAsync(loginName);
    if (user is null) user = await userManager.FindByEmailAsync(loginName);
    if (user is null) { await WriteJson(ctx, 401, "{\"error\":\"Invalid email or password\"}"); return; }

    var identity = user.UserName ?? user.Email ?? loginName;
    var result = await signInManager.PasswordSignInAsync(identity, password, false, lockoutOnFailure: true);
    if (!result.Succeeded) { await WriteJson(ctx, 401, "{\"error\":\"Invalid email or password\"}"); return; }

    var roles = await userManager.GetRolesAsync(user);
    var hours = 8;
    var token = JwtOf(cfg).Issue(user.Id, hours);
    var rolesJson = new StringBuilder();
    foreach (var r in roles)
    {
        if (rolesJson.Length > 0) rolesJson.Append(",");
        rolesJson.Append("\"").Append(Jq(r)).Append("\"");
    }
    var body = "{\"token\":\"" + token
        + "\",\"tokenType\":\"Bearer\",\"expiresIn\":" + (hours * 3600)
        + ",\"user\":{\"id\":\"" + Jq(user.Id)
        + "\",\"email\":\"" + Jq(user.Email ?? "")
        + "\",\"name\":\"" + Jq(user.UserName ?? "")
        + "\",\"roles\":[" + rolesJson + "]}}";
    await WriteJson(ctx, 200, body);
}).AllowAnonymous();

// ── GET /api/items?search= ──
app.MapGet("/api/items", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var itemService = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IItemService>();
    var search = Query(ctx, "search");
    var items = string.IsNullOrWhiteSpace(search)
        ? await itemService.GetAllAsync()
        : await itemService.SearchAsync(search.Trim());
    var cards = items.Select(i => new ItemCard(i.Id.ToString(), i.Code, i.NameAr, i.NameEn, i.SalePrice, i.CostPrice, i.CurrentStock, i.Barcode, i.UnitNameAr, i.ImageUrl)).ToList();
    await WriteJson(ctx, 200, JsonItems(cards));
});

// ── GET /api/customers?search= ──
app.MapGet("/api/customers", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var customerService = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<ICustomerService>();
    var search = Query(ctx, "search");
    var customers = string.IsNullOrWhiteSpace(search)
        ? await customerService.GetAllAsync()
        : await customerService.SearchAsync(search.Trim());
    var cards = customers.Select(c => new CustomerCard(c.Id.ToString(), c.Code, c.NameAr, c.NameEn, c.Phone, c.CurrentBalance)).ToList();
    await WriteJson(ctx, 200, JsonCustomers(cards));
});

// ── POST /api/sales-invoices (يكتب JSON يدوياً) ──
app.MapPost("/api/sales-invoices", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }

    using var buffer = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(buffer);
        buffer.Position = 0;
        using var rd = new StreamReader(buffer);
        var sbJson = new StringBuilder();
        string? line;
        while ((line = rd.ReadLine()) is not null) sbJson.Append(line);
        System.Text.Json.JsonElement root;
        try { root = System.Text.Json.JsonDocument.Parse(sbJson.ToString()).RootElement; }
        catch { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }

    var hasLines = root.TryGetProperty("lines", out var linesVal);
    var hasWarehouse = root.TryGetProperty("warehouseId", out var whVal);
    if (!hasLines || !hasWarehouse) { await WriteJson(ctx, 400, "{\"error\":\"warehouseId and lines are required\"}"); return; }

    // 🔑 مفتاح التكرار من العميل: إن كُرِّر الطلب نُعيد الفاتورة نفسها بلا إنشاء ثانٍ
    var clientKey = root.TryGetProperty("clientRequestId", out var ckNode) ? (ckNode.GetString() ?? "").Trim() : "";
    var photoBase64 = root.TryGetProperty("photoBase64", out var pbNode) ? (pbNode.GetString() ?? "").Trim() : "";
    var photoPath2 = "";
    if (photoBase64.Length > 0) photoPath2 = SavePhotoFile(photoBase64);

    var lines = new List<CreateSalesInvoiceLineDto>();
    foreach (var ln in linesVal.EnumerateArray())
    {
        lines.Add(new CreateSalesInvoiceLineDto
        {
            ItemId = Guid.Parse(ln.GetProperty("itemId").GetString()),
            Quantity = ln.GetProperty("quantity").GetDecimal(),
            UnitPrice = ln.TryGetProperty("unitPrice", out var up) ? up.GetDecimal() : 0m
        });
    }

    if (!root.TryGetProperty("customerId", out var custNode)) { await WriteJson(ctx, 400, "{\"error\":\"customerId required\"}"); return; }
    var customerId = Guid.Parse(custNode.GetString());
    var note = root.TryGetProperty("note", out var nt) ? (nt.GetString() ?? "Mobile") : "Mobile";
    if (!string.IsNullOrEmpty(clientKey)) note = "[SyncKey:" + clientKey + "]" + note;
    if (photoPath2.Length > 0) note = "[\u0635\u0648\u0631\u0629:" + photoPath2 + "]" + note;
    var dto = new CreateSalesInvoiceDto
    {
        CustomerId = customerId,
        WarehouseId = Guid.Parse(whVal.GetString()),
        InvoiceDate = DateTime.Today,
        InvoiceType = (int)SalesInvoiceType.Cash,
        PaymentMethod = 1,
        DiscountPercentage = root.TryGetProperty("discountPercentage", out var dp) ? dp.GetDecimal() : 0m,
        TaxRate = root.TryGetProperty("taxRate", out var tr) ? tr.GetDecimal() : 0m,
        Note = note,
        IsPos = false,
        Lines = lines
    };

    var invoiceService = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<ISalesInvoiceService>();

    // 🔁 مفتاح مكرر؟ ← أعد الفاتورة المخزونة (لا إنشاء ثانٍ)
    if (!string.IsNullOrEmpty(clientKey))
    {
        var existingId = _idem.TryGetValue(clientKey, out var ex) ? ex : "";
        if (!string.IsNullOrEmpty(existingId))
        {
            var existing = await invoiceService.GetByIdAsync(Guid.Parse(existingId));
            if (existing is not null)
            {
                var json = InvoiceJson(existing);
                await WriteJson(ctx, 200, json.Substring(0, json.Length - 1) + ",\"duplicate\":true,\"clientRequestId\":\"" + Jq(clientKey) + "\"}");
                return;
            }
        }
    }

    try
    {
        var invoice = await invoiceService.CreateAsync(dto);
        if (!string.IsNullOrEmpty(clientKey)) _idem[clientKey] = invoice.Id.ToString();

        
        var json = InvoiceJson(invoice);
        if (photoPath2.Length > 0)
            json = json.Substring(0, json.Length - 1) + ",\"photoPath\":\"" + Jq(photoPath2) + "\"}";
        await WriteJson(ctx, 201, json);
    }
    catch (Exception ex)
    {
        // متأخر/متنازع — بانتظار مراجعة يدوية بدل رفض صامت أو قبول أعمى
        if (!string.IsNullOrEmpty(clientKey))
        {
            await WriteReviewFile(clientKey, root.ToString(), ex.Message);
            await WriteJson(ctx, 201, "{\"id\":\"\",\"invoiceNumber\":\"\",\"needsReview\":true,\"reason\":" + Jq(ex.Message) + ",\"clientRequestId\":\"" + Jq(clientKey) + "\"}");
            return;
        }
        await WriteJson(ctx, 422, "{\"error\":" + Jq(ex.Message) + "}");
    }
});

// ── GET /api/sales-returns/lookup?invoiceNumber= (فاتورة مرحّلة للرد + الأسطر القابلة للرد) ──
app.MapGet("/api/sales-returns/lookup", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }

    var text = (Query(ctx, "invoiceNumber") ?? "").Trim();
    if (text.Length == 0) { await WriteJson(ctx, 400, "{\"error\":\"invoiceNumber required\"}"); return; }

    var scope = ctx.RequestServices.CreateScope().ServiceProvider;
    var invoiceService = scope.GetRequiredService<ISalesInvoiceService>();
    var returnService = scope.GetRequiredService<ISalesReturnService>();
    var itemService = scope.GetRequiredService<IItemService>();

    var hits = await invoiceService.SearchPostableInvoicesAsync(text, null, null, 5);
    if (hits.Count == 0) { await WriteJson(ctx, 404, "{\"error\":\"لا توجد فاتورة مرحلة بهذا الرقم\"}"); return; }
    var invoice = await invoiceService.GetByIdAsync(hits[0].Id);
    if (invoice is null) { await WriteJson(ctx, 404, "{\"error\":\"الفاتورة غير موجودة\"}"); return; }

    var remaining = await returnService.GetRemainingReturnableAsync(invoice.Id);

    var linesJson = new StringBuilder();
    if (invoice.Lines is not null)
    {
        foreach (var l in invoice.Lines)
        {
            var it = await itemService.GetByIdAsync(l.ItemId);
            if (linesJson.Length > 0) linesJson.Append(",");
            linesJson.Append("{\"itemId\":\"").Append(l.ItemId)
                .Append("\",\"code\":\"").Append(it is null ? "" : Jq(it.Code))
                .Append("\",\"nameAr\":\"").Append(it is null ? "" : Jq(it.NameAr))
                .Append("\",\"quantity\":").Append(l.Quantity.ToString())
                .Append(",\"unitPrice\":").Append(l.UnitPrice.ToString())
                .Append(",\"remaining\":").Append((remaining.TryGetValue(l.ItemId, out var r) ? r : l.Quantity).ToString()).Append("}");
        }
    }

    var json = "{\"invoiceId\":\"" + invoice.Id
        + "\",\"invoiceNumber\":\"" + Jq(invoice.InvoiceNumber)
        + "\",\"customerId\":\"" + invoice.CustomerId
        + "\",\"customerName\":\"" + Jq(invoice.CustomerName)
        + "\",\"warehouseId\":\"" + invoice.WarehouseId
        + "\",\"warehouseName\":\"" + Jq(invoice.WarehouseName)
        + "\",\"invoiceDate\":\"" + invoice.InvoiceDate.ToString("yyyy-MM-dd")
        + "\",\"totalAmount\":" + invoice.TotalAmount.ToString()
        + ",\"lines\":[" + linesJson.ToString() + "]}";
    await WriteJson(ctx, 200, json);
});

// ── POST /api/sales-returns (مردود مبيعات: أسطر بكميات مقابل فاتورة أصلية مرحّلة) ──
app.MapPost("/api/sales-returns", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }

    using var buffer = new MemoryStream();
    await ctx.Request.Body.CopyToAsync(buffer);
    buffer.Position = 0;
    using var rd = new StreamReader(buffer);
    var sbJson = new StringBuilder();
    string? line;
    while ((line = rd.ReadLine()) is not null) sbJson.Append(line);
    System.Text.Json.JsonElement root;
    try { root = System.Text.Json.JsonDocument.Parse(sbJson.ToString()).RootElement; }
    catch { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }

    if (!root.TryGetProperty("salesInvoiceId", out var invNode) || !root.TryGetProperty("warehouseId", out var whNode))
    { await WriteJson(ctx, 400, "{\"error\":\"salesInvoiceId and warehouseId are required\"}"); return; }
    if (!root.TryGetProperty("lines", out var linesVal)) { await WriteJson(ctx, 400, "{\"error\":\"lines are required\"}"); return; }

    var dtoLines = new List<CreateSalesReturnLineDto>();
    foreach (var ln in linesVal.EnumerateArray())
    {
        dtoLines.Add(new CreateSalesReturnLineDto
        {
            ItemId = Guid.Parse(ln.GetProperty("itemId").GetString()),
            Quantity = ln.GetProperty("quantity").GetDecimal(),
            UnitPrice = 0m
        });
    }

    var dto = new CreateSalesReturnDto
    {
        SalesInvoiceId = Guid.Parse(invNode.GetString()),
        WarehouseId = Guid.Parse(whNode.GetString()),
        ReturnDate = DateTime.Today,
        Note = root.TryGetProperty("note", out var nt) ? (nt.GetString() ?? "") : "",
        Lines = dtoLines
    };

    try
    {
        var returnService = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<ISalesReturnService>();
        var created = await returnService.CreateAsync(dto);
        var json = "{\"id\":\"" + created.Id
            + "\",\"returnNumber\":\"" + Jq(created.ReturnNumber)
            + "\",\"invoiceNumber\":\"" + Jq(created.InvoiceNumber)
            + "\",\"totalAmount\":" + created.TotalAmount.ToString()
            + ",\"lineCount\":" + (created.Lines is null ? 0 : created.Lines.Count).ToString() + "}";
        await WriteJson(ctx, 201, json);
    }
    catch (Exception ex)
    {
        await WriteJson(ctx, 422, "{\"error\":" + Jq(ex.Message) + "}");
    }
});

// ── GET /api/sales-invoices/{id:guid} ──
app.MapGet("/api/sales-invoices/{id:guid}", async (HttpContext ctx, Guid id) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var invoice = await ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<ISalesInvoiceService>().GetByIdAsync(id);
    if (invoice is null) { await WriteJson(ctx, 404, "{\"error\":\"Invoice not found\"}"); return; }
    await WriteJson(ctx, 200, InvoiceJson(invoice));
});

// ── GET /api/warehouses ──
app.MapGet("/api/warehouses", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var warehouses = await ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IWarehouseService>().GetAllAsync();
    var cards = warehouses.Select(w => new WarehouseCard(w.Id.ToString(), w.Code, w.NameAr, w.NameEn, w.IsActive)).ToList();
    await WriteJson(ctx, 200, JsonWarehouses(cards));
});

// ── عهدة المندوبين (المرحلة 3) ──
app.MapPost("/api/reps/custody", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    var u = await RequireUserAsync(ctx, cfg);
    if (u is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }

    using var buffer = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(buffer);
        buffer.Position = 0;
        using var rd = new StreamReader(buffer);
        var sbJson = new StringBuilder();
        string? line;
        while ((line = rd.ReadLine()) is not null) sbJson.Append(line);
        System.Text.Json.JsonElement root;
        try { root = System.Text.Json.JsonDocument.Parse(sbJson.ToString()).RootElement; }
        catch { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }

    var amount = root.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0m;
    if (amount <= 0m) { await WriteJson(ctx, 400, "{\"error\":\"amount required\"}"); return; }

    Guid? customerId = null;
    if (root.TryGetProperty("customerId", out var cNode)) { try { customerId = Guid.Parse(cNode.GetString()); } catch { } }
    Guid? invoiceId = null;
    if (root.TryGetProperty("invoiceId", out var iNode)) { try { invoiceId = Guid.Parse(iNode.GetString()); } catch { } }
    var note = root.TryGetProperty("note", out var nt) ? (nt.GetString() ?? "") : "";
    var paymentMethod = root.TryGetProperty("paymentMethod", out var pm) ? pm.GetInt32() : 1;

    var dto = new ERPSystem.Application.DTOs.RepCustody.CreateRepCustodyDto
    {
        Effect = ERPSystem.Domain.Enums.RepCustodyEffect.Collect,
        RepUserId = u.Id,
        CustomerId = customerId,
        InvoiceId = invoiceId,
        PaymentMethod = paymentMethod,
        Amount = amount,
        Notes = note,
        Timestamp = DateTime.Now
    };
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IRepCustodyService>();
    try
    {
        var r = await svc.CreateAsync(dto, u.Id);
        await WriteJson(ctx, 201, CustodyJson(r));
    }
    catch (Exception ex) { await WriteJson(ctx, 422, "{\"error\":" + Jq(ex.Message) + "}"); }
});

// ── أرصدة عهود المندوبين ──
app.MapGet("/api/reps/custody/balances", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IRepCustodyService>();
    var balances = await svc.GetBalancesAsync();
    var sb2 = new StringBuilder("[");
    foreach (var b in balances)
    {
        if (sb2.Length > 1) sb2.Append(",");
        sb2.Append("{\"repUserId\":\"").Append(Jq(b.RepUserId)).Append("\",\"balance\":").Append(b.Balance.ToString()).Append("}");
    }
    await WriteJson(ctx, 200, sb2.Append("]").ToString());
});

// ── تأكيد استلام العهدة لدى المكتب (تصفير) ──
app.MapPost("/api/reps/custody/deliver", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    var u = await RequireUserAsync(ctx, cfg);
    if (u is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }

    using var buffer = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(buffer);
        buffer.Position = 0;
        using var rd = new StreamReader(buffer);
        var sbJson = new StringBuilder();
        string? line;
        while ((line = rd.ReadLine()) is not null) sbJson.Append(line);
        System.Text.Json.JsonElement root;
        try { root = System.Text.Json.JsonDocument.Parse(sbJson.ToString()).RootElement; }
        catch { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }

    var repUserId = root.TryGetProperty("repUserId", out var ru) ? (ru.GetString() ?? "") : "";
    var amount = root.TryGetProperty("amount", out var amt2) ? amt2.GetDecimal() : 0m;
    var note = root.TryGetProperty("note", out var nt2) ? (nt2.GetString() ?? "") : "";
    if (string.IsNullOrWhiteSpace(repUserId) || amount <= 0m) { await WriteJson(ctx, 400, "{\"error\":\"repUserId and amount required\"}"); return; }

    var dto = new ERPSystem.Application.DTOs.RepCustody.CreateRepCustodyDto
    {
        Effect = ERPSystem.Domain.Enums.RepCustodyEffect.Delivery,
        RepUserId = repUserId,
        Amount = amount,
        Notes = note,
        Timestamp = DateTime.Now
    };
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IRepCustodyService>();
    try
    {
        var r = await svc.CreateAsync(dto, u.Id);
        await WriteJson(ctx, 201, CustodyJson(r));
    }
    catch (Exception ex) { await WriteJson(ctx, 422, "{\"error\":" + Jq(ex.Message) + "}"); }
});

// ── منسّق الرفوف (المرحلة 4 — الشرحة العمودية) ──
app.MapGet("/api/shelf/par-levels", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IShelfService>();
    var rows = await svc.GetParLevelsAsync(null);
    var sb = new StringBuilder("[");
    foreach (var p in rows)
    {
        if (sb.Length > 1) sb.Append(",");
        sb.Append("{\"id\":\"").Append(p.Id).Append("\",\"itemId\":\"").Append(p.ItemId)
          .Append("\",\"location\":\"").Append(Jq(p.Location))
          .Append("\",\"parLevel\":").Append(p.ParLevel.ToString()).Append("}");
    }
    await WriteJson(ctx, 200, sb.Append("]").ToString());
});

app.MapPost("/api/shelf/checks", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    var u = await RequireUserAsync(ctx, cfg);
    if (u is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var maybeBody = ParseBodyFull(ctx);
    if (maybeBody is null) { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }
    var root = (System.Text.Json.JsonElement)maybeBody;
    var itemId = ParseGuid(root, "itemId");
    if (itemId is null) { await WriteJson(ctx, 400, "{\"error\":\"itemId required\"}"); return; }
    var dto = new ERPSystem.Application.DTOs.Shelf.CreateShelfCheckDto
    {
        ItemId = itemId,
        Location = root.TryGetProperty("location", out var lo) ? (lo.GetString() ?? "الرئيسية") : "الرئيسية",
        ObservedQty = root.TryGetProperty("observedQty", out var oq) ? oq.GetDecimal() : 0m,
        ParLevel = null,
        PhotoOneBase64 = root.TryGetProperty("photoOne", out var ph1) ? ph1.GetString() : null,
        PhotoTwoBase64 = root.TryGetProperty("photoTwo", out var ph2) ? ph2.GetString() : null,
        Notes = root.TryGetProperty("notes", out var n1) ? n1.GetString() : null
    };
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IShelfService>();
    try
    {
        var c = await svc.CreateShelfCheckAsync(dto, u.Id);
        await WriteJson(ctx, 201, "{\"id\":\"" + c.Id + "\",\"itemId\":\"" + c.ItemId + "\",\"location\":\"" + Jq(c.Location) + "\",\"observedQty\":" + c.ObservedQty.ToString() + ",\"photoOne\":\"" + (c.PhotoOnePath ?? "") + "\"}");
    }
    catch (Exception ex) { await WriteJson(ctx, 422, "{\"error\":" + Jq(ex.Message) + "}"); }
});

app.MapGet("/api/shelf/checks", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IShelfService>();
    var rows = await svc.GetShelfChecksAsync(ParseGuidStr(Query(ctx, "itemId")), Query(ctx, "location"));
    var sb = new StringBuilder("[");
    foreach (var c in rows)
    {
        if (sb.Length > 1) sb.Append(",");
        sb.Append("{\"id\":\"").Append(c.Id).Append("\",\"itemId\":\"").Append(c.ItemId).Append("\",\"location\":\"").Append(Jq(c.Location))
          .Append("\",\"observedQty\":").Append(c.ObservedQty.ToString())
          .Append(",\"checkedAt\":\"").Append(c.CheckedAt.ToString("yyyy-MM-dd'T'HH:mm")).Append("\"}");
    }
    await WriteJson(ctx, 200, sb.Append("]").ToString());
});

app.MapGet("/api/shelf/expiry-warnings", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IShelfService>();
    var rows = await svc.GetExpiryWarningsAsync(ParseGuidStr(Query(ctx, "itemId")));
    var sb = new StringBuilder("[");
    foreach (var w in rows)
    {
        if (sb.Length > 1) sb.Append(",");
        sb.Append("{\"itemId\":\"").Append(w.ItemId).Append("\",\"itemCode\":\"").Append(Jq(w.ItemCode))
          .Append("\",\"itemNameAr\":\"").Append(Jq(w.ItemNameAr))
          .Append("\",\"batchNumber\":\"").Append(Jq(w.BatchNumber))
          .Append("\",\"expiryDate\":\"").Append(w.ExpiryDate.ToString()).Append("\",\"remQty\":").Append(w.RemainingQty.ToString())
          .Append(",\"daysLeft\":").Append(w.DaysLeft).Append("}");
    }
    await WriteJson(ctx, 200, sb.Append("]").ToString());
});

app.MapPost("/api/shelf/price-captures", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    var u = await RequireUserAsync(ctx, cfg);
    if (u is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var maybeBody = ParseBodyFull(ctx);
    if (maybeBody is null) { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }
    var root = (System.Text.Json.JsonElement)maybeBody;
    var itemId = ParseGuid(root, "itemId");
    if (itemId is null) { await WriteJson(ctx, 400, "{\"error\":\"itemId required\"}"); return; }
    var dto = new ERPSystem.Application.DTOs.Shelf.CreateShelfPriceCaptureDto
    {
        ItemId = itemId,
        CompetitorName = root.TryGetProperty("competitorName", out var cn) ? (cn.GetString() ?? "أخرى") : "أخرى",
        Price = root.TryGetProperty("price", out var pr) ? pr.GetDecimal() : 0m,
        PhotoBase64 = root.TryGetProperty("photo", out var ph) ? ph.GetString() : null,
        Notes = root.TryGetProperty("notes", out var n2) ? n2.GetString() : null
    };
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IShelfService>();
    try
    {
        var c = await svc.CreatePriceCaptureAsync(dto, u.Id);
        await WriteJson(ctx, 201, "{\"id\":\"" + c.Id + "\",\"itemId\":\"" + c.ItemId + "\",\"competitorName\":\"" + Jq(c.CompetitorName) + "\",\"price\":" + c.Price.ToString() + "}");
    }
    catch (Exception ex) { await WriteJson(ctx, 422, "{\"error\":" + Jq(ex.Message) + "}"); }
});

app.MapGet("/api/shelf/price-captures", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IShelfService>();
    var rows = await svc.GetPriceCapturesAsync();
    var sb = new StringBuilder("[");
    foreach (var c in rows)
    {
        if (sb.Length > 1) sb.Append(",");
        sb.Append("{\"id\":\"").Append(c.Id).Append("\",\"itemId\":\"").Append(c.ItemId).Append("\",\"competitor\":\"").Append(Jq(c.CompetitorName))
          .Append("\",\"price\":").Append(c.Price.ToString())
          .Append(",\"capturedAt\":\"").Append(c.CapturedAt.ToString("yyyy-MM-dd'T'HH:mm")).Append("\"}");
    }
    await WriteJson(ctx, 200, sb.Append("]").ToString());
});

// ── سند إتلاف ميداني (يعيد استخدام StockWriteOffService الموجود — لا إعادة بناء منطق) ──
app.MapPost("/api/write-offs", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    var u = await RequireUserAsync(ctx, cfg);
    if (u is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var maybeBody = ParseBodyFull(ctx);
    if (maybeBody is null) { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }
    var root = (System.Text.Json.JsonElement)maybeBody;
    var itemId = ParseGuid(root, "itemId");
    var warehouseId = ParseGuid(root, "warehouseId");
    var batchId = ParseGuid(root, "batchId");
    if (itemId is null || warehouseId is null) { await WriteJson(ctx, 400, "{\"error\":\"itemId and warehouseId required\"}"); return; }
    var dto = new ERPSystem.Application.DTOs.Inventory.CreateStockWriteOffDto
    {
        ItemId = (Guid)itemId,
        WarehouseId = (Guid)warehouseId,
        BatchId = batchId,
        Quantity = root.TryGetProperty("quantity", out var q3) ? q3.GetDecimal() : 0m,
        Reason = root.TryGetProperty("reason", out var rs) ? rs.GetInt32() : 1,
        OtherReasonText = root.TryGetProperty("otherReasonText", out var ot) ? ot.GetString() : null,
        Notes = root.TryGetProperty("notes", out var n3) ? n3.GetString() : null
    };
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IStockWriteOffService>();
    try
    {
        var c = await svc.CreateAsync(dto, u.Id);
        await WriteJson(ctx, 201, "{\"id\":\"" + c.Id + "\",\"documentNumber\":\"" + Jq(c.DocumentNumber) + "\",\"itemId\":\"" + c.ItemId + "\",\"quantity\":" + c.Quantity.ToString() + ",\"reason\":" + c.Reason + "}");
    }
    catch (Exception ex) { await WriteJson(ctx, 422, "{\"error\":" + Jq(ex.Message) + "}"); }
});

// ── إعداد حدود الرف (مكتب/إدارة) ──
app.MapPost("/api/shelf/par-levels", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    var u = await RequireUserAsync(ctx, cfg);
    if (u is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var maybeBody = ParseBodyFull(ctx);
    if (maybeBody is null) { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }
    var root = (System.Text.Json.JsonElement)maybeBody;
    var itemId = ParseGuid(root, "itemId");
    var location = root.TryGetProperty("location", out var lo2) ? (lo2.GetString() ?? "الرئيسية") : "الرئيسية";
    var parLevel = root.TryGetProperty("parLevel", out var pl) ? pl.GetDecimal() : 0m;
    if (itemId is null || parLevel <= 0m) { await WriteJson(ctx, 400, "{\"error\":\"itemId and parLevel required\"}"); return; }
    var db = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<ERPSystem.Infrastructure.Data.AppDbContext>();
    var row = new ERPSystem.Domain.Entities.ShelfParLevel
    {
        Id = Guid.NewGuid(),
        ItemId = (Guid)itemId,
        Location = location,
        ParLevel = parLevel,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    db.Set<ERPSystem.Domain.Entities.ShelfParLevel>().Add(row);
    await db.SaveChangesAsync();
    await WriteJson(ctx, 201, "{\"id\":\"" + row.Id + "\",\"itemId\":\"" + row.ItemId + "\",\"location\":\"" + Jq(row.Location) + "\",\"parLevel\":" + row.ParLevel.ToString() + "}");
});

// ── لوحات المدير (المرحلة 5 — قراءة من البيانات المتراكمة) ──
app.MapGet("/api/shelf/activity", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IShelfService>();
    var rows = await svc.GetFieldActivityAsync();
    var sb = new StringBuilder("[");
    foreach (var a in rows)
    {
        if (sb.Length > 1) sb.Append(",");
        sb.Append("{\"userId\":\"").Append(Jq(a.UserId)).Append("\",\"checks\":").Append(a.ShelfChecks)
          .Append(",\"captures\":").Append(a.PriceCaptures)
          .Append(",\"collections\":").Append(a.RepCollections).Append("}");
    }
    await WriteJson(ctx, 200, sb.Append("]").ToString());
});

app.MapGet("/api/shelf/shortfalls", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    if (await RequireUserAsync(ctx, cfg) is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    var svc = ctx.RequestServices.CreateScope().ServiceProvider.GetRequiredService<IShelfService>();
    var rows = await svc.GetShortfallsAsync();
    var sb = new StringBuilder("[");
    foreach (var s in rows)
    {
        if (sb.Length > 1) sb.Append(",");
        sb.Append("{\"itemId\":\"").Append(s.ItemId).Append("\",\"location\":\"").Append(Jq(s.Location))
          .Append("\",\"parLevel\":").Append(s.ParLevel.ToString())
          .Append(",\"belowCount\":").Append(s.BelowCount)
          .Append(",\"avgObserved\":").Append(s.AvgObserved.ToString()).Append("}");
    }
    await WriteJson(ctx, 200, sb.Append("]").ToString());
});

// ── صفحة ترحيب الـAPI (ليست واجهة — واجهة النظام على :5186 والتطبيق نافذة MAUI) ──
app.MapGet("/", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    await WriteJson(ctx, 200, "{\"name\":\"ERPSystem REST API\",\"version\":\"1.0\",\"ui\":\"http://localhost:5186\",\"mobile\":\"MAUI desktop app\",\"endpoints\":\"/api\"}");
});

app.MapGet("/api", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    await WriteJson(ctx, 200, "{\"endpoints\":[\"POST /api/auth/login\",\"GET /api/items\",\"GET /api/customers\",\"GET /api/warehouses\",\"POST /api/sales-invoices\",\"GET /api/sales-invoices/{id}\",\"POST /api/reps/custody\",\"GET /api/reps/custody/balances\",\"POST /api/reps/custody/deliver\",\"GET /api/shelf/par-levels\",\"POST /api/shelf/par-levels\",\"POST /api/shelf/checks\",\"GET /api/shelf/checks\",\"GET /api/shelf/expiry-warnings\",\"POST /api/shelf/price-captures\",\"GET /api/shelf/price-captures\",\"GET /api/shelf/activity\",\"GET /api/shelf/shortfalls\",\"POST /api/write-offs\",\"GET /health\"]}");
});

app.MapGet("/health", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    await WriteJson(ctx, 200, "{\"status\":\"ok\",\"service\":\"erp-api\",\"time\":\"" + DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") + "\"}");
});

// ── إنشاء مستخدم/مندوب/منسّق (مدير النظام فقط — عبر آلية الهوية الرسمية) ──
app.MapPost("/api/admin/users", async (HttpContext ctx) =>
{
    var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
    ApplyCors(ctx, cfg);
    var u = await RequireUserAsync(ctx, cfg);
    if (u is null) { await WriteJson(ctx, 401, "{\"error\":\"Unauthorized\"}"); return; }
    if (!u.Roles.Contains("Admin") && !u.Roles.Contains("SuperAdmin")) { await WriteJson(ctx, 403, "{\"error\":\"Admin required\"}"); return; }
    var maybeBody = ParseBodyFull(ctx);
    if (maybeBody is null) { await WriteJson(ctx, 400, "{\"error\":\"Invalid JSON body\"}"); return; }
    var root = (System.Text.Json.JsonElement)maybeBody;

    var userName = root.TryGetProperty("userName", out var un) ? (un.GetString() ?? "") : "";
    var email = root.TryGetProperty("email", out var em) ? (em.GetString() ?? "") : "";
    var password = root.TryGetProperty("password", out var pw) ? (pw.GetString() ?? "") : "";
    if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || password.Length < 8)
    {
        await WriteJson(ctx, 400, "{\"error\":\"userName, email, password (>=8) required\"}");
        return;
    }
    var roles = new List<string>();
    if (root.TryGetProperty("roles", out var rl))
    {
        foreach (var r in rl.EnumerateArray())
        {
            var rr = r.GetString();
            if (!string.IsNullOrWhiteSpace(rr)) roles.Add(rr);
        }
    }

    var um = ctx.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
    var user = new IdentityUser { UserName = userName, Email = email, EmailConfirmed = true };
    var res = await um.CreateAsync(user, password);
    if (!res.Succeeded)
    {
        // محايد: إن كان المستخدم موجوداً (مستخدماً/منسّقاً) نضيف الأدوار المطلوبة له
        var existing = await um.FindByNameAsync(userName);
        if (existing is null)
        {
            await WriteJson(ctx, 422, "{\"error\":" + Jq(string.Join("; ", res.Errors.Select(e => e.Description))) + "}");
            return;
        }
        user = existing;
    }
    if (roles.Count > 0)
    {
        var addRoles = await um.AddToRolesAsync(user, roles);
        if (!addRoles.Succeeded)
        {
            await WriteJson(ctx, 422, "{\"error\":" + Jq(string.Join("; ", addRoles.Errors.Select(e => e.Description))) + "}");
            return;
        }
    }
    await WriteJson(ctx, 201, "{\"id\":\"" + user.Id + "\",\"email\":\"" + Jq(email) + "\",\"roles\":[\"" + string.Join("\",\"", roles) + "\"]}");
});

app.Run();

// ══ أدوات مساعدة ══
static JwtService JwtOf(IConfiguration cfg)
{
    var secretHex = cfg["ERPSystem.Api.JwtSecret"] ?? "";
    if (string.IsNullOrWhiteSpace(secretHex))
        throw new InvalidOperationException("ERPSystem.Api.JwtSecret غير مهيّأ (سادس-عشري) — نفّذ: dotnet user-secrets set ERPSystem.Api.JwtSecret \"...\"");
    return new JwtService(secretHex, "erp.api");
}

static async Task<ApiUser?> RequireUserAsync(HttpContext ctx, IConfiguration cfg)
{
    var um = ctx.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
    var rm = ctx.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
    return await ApiAuth.RequireUserAsync(ctx, JwtOf(cfg), um, rm);
}

static ApiError Unauthorized(HttpContext ctx)
{
    ctx.Response.StatusCode = 401;
    return new ApiError("Unauthorized");
}

static string? Query(HttpContext ctx, string name)
    => ctx.Request.Query[name].FirstOrDefault();

static async Task WriteJson(HttpContext ctx, int status, string json)
{
    ctx.Response.StatusCode = status;
    ctx.Response.ContentType = "application/json; charset=utf-8";
    await ctx.Response.WriteAsync(json);
}

static Guid? ParseGuidStr(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return null;
    try { return Guid.Parse(raw.Trim()); }
    catch { return null; }
}

static Guid? ParseGuid(System.Text.Json.JsonElement root, string key)
{
    if (!root.TryGetProperty(key, out var n)) return null;
    try { return Guid.Parse(n.GetString()); }
    catch { return null; }
}

static System.Text.Json.JsonElement? ParseBodyFull(HttpContext ctx)
{
    try
    {
        using var buffer = new MemoryStream();
        ctx.Request.Body.CopyToAsync(buffer).GetAwaiter().GetResult();
        buffer.Position = 0;
        using var rd = new StreamReader(buffer);
        var sb = new StringBuilder();
        string? line;
        while ((line = rd.ReadLine()) is not null) sb.Append(line);
        if (sb.Length == 0) return null;
        return System.Text.Json.JsonDocument.Parse(sb.ToString()).RootElement;
    }
    catch { return null; }
}

static string SavePhotoFile(string base64)
{
    try
    {
        var bytes = Convert.FromBase64String(base64);
        if (bytes.Length == 0) return "";
        var path = $"D:/erp_inv_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg";
        var f = File.OpenWrite(path);
        f.Write(bytes);
        f.Flush();
        f.Dispose();
        return path;
    }
    catch { return ""; }
}

static string Jq(string s)
{
    var chars = s.ToCharArray();
    var sb = new StringBuilder();
    foreach (var c in chars)
    {
        var cp = (int)c;
        if (c == '\\') sb.Append("\\\\");
        else if (c == '"') sb.Append("\\\"");
        else if (cp < 0x20) sb.Append("\\u00").Append(Hex2(cp));
        else if (cp < 0x7F) sb.Append(c);
        else
        {
            // أي حرف غير ASCII → \uXXXX (يبقي المخرج JSON نصاً ASCII خالصاً ليتحلّل لدى العميل بأمان)
            sb.Append("\\u").Append(HexN(cp, 4));
        }
    }
    return sb.ToString();
}

static string HexN(int v, int digits)
{
    var s = "";
    while (v > 0) { s = "0123456789abcdef" [v & 0xF] + s; v >>= 4; }
    while (s.Length < digits) s = "0" + s;
    return s;
}

static string Hex2(int v) => HexN(v, 2);

/// <summary>يحفظ طلباً بانتظار المراجعة اليدوية (ملف JSON مسطح على القرص) — لا فقدان ولا صمت.</summary>
static async Task WriteReviewFile(string clientKey, string payload, string reason)
{
    try
    {
        var body = "{\"clientRequestId\":\"" + Jq(clientKey) + "\",\"reason\":" + Jq(reason) + ",\"payload\":" + payload + "}";
        var f = File.OpenWrite("D:/erp_review_" + clientKey + ".json");
        f.Write(Utf8Bytes(body));
        f.Flush();
        f.Dispose();
    }
    catch { }
}

/// <summary>ترميز UTF-8 يدوي (يقبل العربية) لملفات المراجعة.</summary>
static byte[] Utf8Bytes(string s)
{
    var chars = s.ToCharArray();
    var buf = new List<byte>();
    foreach (var c in chars)
    {
        var cp = (int)c;
        if (cp < 0x80) buf.Add((byte)cp);
        else if (cp < 0x800) { buf.Add((byte)(0xC0 | (cp >> 6))); buf.Add((byte)(0x80 | (cp & 0x3F))); }
        else { buf.Add((byte)(0xE0 | (cp >> 12))); buf.Add((byte)(0x80 | ((cp >> 6) & 0x3F))); buf.Add((byte)(0x80 | (cp & 0x3F))); }
    }
    var bytesOut = new byte[buf.Count];
    for (int i = 0; i < buf.Count; i++) bytesOut[i] = buf[i];
    return bytesOut;
}

static string JsonItems(IEnumerable<ItemCard> cards)
{
    var sb = new StringBuilder("[");
    foreach (var c in cards)
    {
        if (sb.Length > 1) sb.Append(",");
        sb.Append("{\"id\":\"").Append(c.Id).Append("\",\"code\":\"").Append(Jq(c.Code))
            .Append("\",\"nameAr\":\"").Append(Jq(c.NameAr))
            .Append("\",\"nameEn\":").Append(c.NameEn is null ? "null" : "\"" + Jq(c.NameEn) + "\"")
            .Append(",\"salePrice\":").Append(c.SalePrice is null ? "0" : c.SalePrice!.ToString())
            .Append(",\"costPrice\":").Append(c.CostPrice is null ? "0" : c.CostPrice!.ToString())
            .Append(",\"currentStock\":").Append(c.CurrentStock.ToString())
            .Append(",\"barcode\":").Append(c.Barcode is null ? "null" : "\"" + Jq(c.Barcode) + "\"")
            .Append(",\"unitNameAr\":").Append(c.UnitNameAr is null ? "null" : "\"" + Jq(c.UnitNameAr) + "\"")
            .Append(",\"imageUrl\":").Append(c.ImageUrl is null ? "null" : "\"" + Jq(c.ImageUrl) + "\"").Append("}");
    }
    return sb.Append("]").ToString();
}

static string JsonCustomers(IEnumerable<CustomerCard> cards)
{
    var sb = new StringBuilder("[");
    foreach (var c in cards)
    {
        if (sb.Length > 1) sb.Append(",");
        sb.Append("{\"id\":\"").Append(c.Id).Append("\",\"code\":\"").Append(Jq(c.Code))
            .Append("\",\"nameAr\":\"").Append(Jq(c.NameAr))
            .Append("\",\"nameEn\":").Append(c.NameEn is null ? "null" : "\"" + Jq(c.NameEn) + "\"")
            .Append(",\"phone\":").Append(c.Phone is null ? "null" : "\"" + Jq(c.Phone) + "\"")
            .Append(",\"currentBalance\":").Append(c.CurrentBalance.ToString()).Append("}");
    }
    return sb.Append("]").ToString();
}

static string InvoiceJson(SalesInvoiceDto i)
{
    var linesJson = new StringBuilder();
    if (i.Lines is not null)
    {
        foreach (var l in i.Lines)
        {
            if (linesJson.Length > 0) linesJson.Append(",");
            linesJson.Append("{\"itemId\":\"").Append(l.ItemId).Append("\",\"quantity\":").Append(l.Quantity.ToString())
                .Append(",\"unitPrice\":").Append(l.UnitPrice.ToString())
                .Append(",\"lineTotal\":").Append(l.LineTotal.ToString()).Append("}");
        }
    }
    return "{\"id\":\"" + i.Id + "\",\"invoiceNumber\":\"" + Jq(i.InvoiceNumber)
        + "\",\"invoiceDate\":\"" + i.InvoiceDate.ToString("yyyy-MM-dd'T'HH:mm")
        + "\",\"customerName\":\"" + Jq(i.CustomerName)
        + "\",\"subTotal\":" + i.SubTotal.ToString()
        + ",\"discountAmount\":" + i.DiscountAmount.ToString()
        + ",\"taxAmount\":" + i.TaxAmount.ToString()
        + ",\"totalAmount\":" + i.TotalAmount.ToString()
        + ",\"note\":" + (i.Note is null ? "null" : "\"" + Jq(i.Note) + "\"")
        + ",\"isPos\":" + (i.IsPos ? "true" : "false")
        + ",\"lines\":[" + linesJson.ToString() + "]}";
}

static string JsonWarehouses(IEnumerable<WarehouseCard> cards)
{
    var sb = new StringBuilder("[");
    foreach (var c in cards)
    {
        if (sb.Length > 1) sb.Append(",");
        sb.Append("{\"id\":\"").Append(c.Id).Append("\",\"code\":\"").Append(Jq(c.Code))
            .Append("\",\"nameAr\":\"").Append(Jq(c.NameAr))
            .Append("\",\"nameEn\":").Append(c.NameEn is null ? "null" : "\"" + Jq(c.NameEn) + "\"")
            .Append(",\"isActive\":").Append(c.IsActive ? "true" : "false").Append("}");
    }
    return sb.Append("]").ToString();
}

static string CustodyJson(ERPSystem.Application.DTOs.RepCustody.RepCustodyDto r)
{
    return "{\"id\":\"" + r.Id.ToString()
        + "\",\"effect\":" + (r.Effect == ERPSystem.Domain.Enums.RepCustodyEffect.Collect ? "1" : "2")
        + ",\"repUserId\":\"" + Jq(r.RepUserId)
        + "\",\"amount\":" + r.Amount.ToString()
        + ",\"entryNumber\":\"" + Jq(r.EntryNumber)
        + "\",\"entryDescription\":\"" + Jq(r.EntryDescription) + "\"}";
}

static void ApplyCors(HttpContext ctx, IConfiguration cfg)
{
    var allowed = (cfg["ERPSystem.Api.CorsOrigins"] ?? "").Split(",");
    var origin = ctx.Request.Headers["Origin"].FirstOrDefault();
    if (origin is not null && allowed.Contains(origin))
    {
        ctx.Response.Headers["Access-Control-Allow-Origin"] = origin;
        ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
        ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        ctx.Response.Headers["Access-Control-Max-Age"] = "86400";
    }
}