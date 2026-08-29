using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.DTOs.Items;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Interfaces;
using ERPSystem.Infrastructure;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

string conn = args.Length > 0 ? args[0] : "Server=(localdb)\\mssqllocaldb;Database=ERPSystemDb_FuncTest;Trusted_Connection=True;TrustServerCertificate=True";

var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:DefaultConnection"] = conn
}).Build();

var services = new ServiceCollection();
services.AddInfrastructure(cfg);
var provider = services.BuildServiceProvider();

Guid CAT_FIN = Guid.Parse("40000000-0000-0000-0000-000000000002");
Guid UNIT_PCS = Guid.Parse("20000000-0000-0000-0000-000000000001");
Guid UNIT_L = Guid.Parse("20000000-0000-0000-0000-000000000003");
Guid UNIT_BOX = Guid.Parse("20000000-0000-0000-0000-000000000004");
Guid WH_MAIN = Guid.Parse("30000000-0000-0000-0000-000000000001");

var customerIds = new List<Guid>();
var supplierIds = new List<Guid>();
var itemIds = new List<Guid>();
var purchaseIds = new List<Guid>();
var salesInfo = new List<(Guid InvoiceId, Guid ItemId, decimal Qty)>();

using (var scope = provider.CreateScope())
{
    var customers = scope.ServiceProvider.GetRequiredService<ICustomerService>();
    var suppliers = scope.ServiceProvider.GetRequiredService<ISupplierService>();
    var items = scope.ServiceProvider.GetRequiredService<IItemService>();
    var purchaseInvoices = scope.ServiceProvider.GetRequiredService<IPurchaseInvoiceService>();
    var salesInvoices = scope.ServiceProvider.GetRequiredService<ISalesInvoiceService>();
    var salesReturns = scope.ServiceProvider.GetRequiredService<ISalesReturnService>();
    var purchaseReturns = scope.ServiceProvider.GetRequiredService<IPurchaseReturnService>();

    // --- 1) العملاء (10) ---
    var customerRows = new (string Code, string Ar)[] {
        ("CUS-SM-01","شركة الأمل التجارية"),("CUS-SM-02","مؤسسة النور"),
        ("CUS-SM-03","سوبر ماركت الرازي"),("CUS-SM-04","بقالة البركة"),
        ("CUS-SM-05","هايبر ماركت المدينة"),("CUS-SM-06","مخازن الصفا"),
        ("CUS-SM-07","وكالة الرؤية"),("CUS-SM-08","أسواق الوفاء"),
        ("CUS-SM-09","تجارة البستان"),("CUS-SM-10","شركة الفجر الغذائية")
    };
    foreach (var c in customerRows)
    {
        var dto = new CreateCustomerDto { Code = c.Code, NameAr = c.Ar, Address = "عمان - الأردن", CreditLimit = 20000 };
        customerIds.Add((await customers.CreateAsync(dto)).Id);
        Console.WriteLine("عميل: " + c.Code + " " + c.Ar);
    }

    // --- 2) الموردون (3) ---
    var supplierRows = new (string Code, string Ar)[] {
        ("SUP-SM-01","مصنع الألبان الحديث"),("SUP-SM-02","شركة التموين الغذائي"),
        ("SUP-SM-03","مستودعات البضائع العامة")
    };
    foreach (var s in supplierRows)
    {
        var dto = new CreateSupplierDto { Code = s.Code, NameAr = s.Ar, Address = "عمان - الأردن", CreditLimit = 50000 };
        supplierIds.Add((await suppliers.CreateAsync(dto)).Id);
        Console.WriteLine("مورد: " + s.Code + " " + s.Ar);
    }

    // --- 3) المنتجات (20) ---
    var productRows = new (string Code, string Ar, string En, Guid Unit, decimal Cost, decimal Sale)[] {
        ("SM-001","حليب طازج كامل الدسم - لتر","Fresh Whole Milk 1L",UNIT_L,2.60m,3.90m),
        ("SM-002","خبز أبيض كبير","White Bread Loaf",UNIT_PCS,0.80m,1.25m),
        ("SM-003","بيض أحمر - 30 بيضة","Red Eggs (30)",UNIT_BOX,3.20m,4.50m),
        ("SM-004","زيت زيتون بكر ممتاز 750 مل","Extra Virgin Olive Oil 750ml",UNIT_PCS,8.50m,12.00m),
        ("SM-005","أرز مصري فاخر 5 كجم","Egyptian Rice 5kg",UNIT_PCS,6.20m,8.50m),
        ("SM-006","دقيق أبيض متعدد الاستخدامات 1 كجم","All-Purpose Flour 1kg",UNIT_PCS,1.25m,1.80m),
        ("SM-007","سكر ناعم أبيض 1 كجم","White Sugar 1kg",UNIT_PCS,1.40m,2.00m),
        ("SM-008","زيت دوار الشمس 1 لتر","Sunflower Oil 1L",UNIT_L,2.90m,4.20m),
        ("SM-009","جبنة بيضاء طرية 500 جم","Fresh White Cheese 500g",UNIT_PCS,3.30m,4.80m),
        ("SM-010","لبنة بلدية 400 جم","Labneh 400g",UNIT_PCS,2.70m,3.90m),
        ("SM-011","قهوة عربية مطحونة 250 جم","Arabic Ground Coffee 250g",UNIT_PCS,7.20m,10.50m),
        ("SM-012","شاي أسود - 100 كيس","Black Tea (100 bags)",UNIT_PCS,4.60m,6.75m),
        ("SM-013","مشروب غازي كولا 330 مل","Cola 330ml",UNIT_PCS,0.65m,1.00m),
        ("SM-014","مياه معدنية 1.5 لتر","Mineral Water 1.5L",UNIT_PCS,0.42m,0.60m),
        ("SM-015","شامبو عائلي 500 مل","Family Shampoo 500ml",UNIT_PCS,3.60m,5.25m),
        ("SM-016","صابون غسيل صلب 1 كجم","Laundry Soap Bar 1kg",UNIT_PCS,2.30m,3.30m),
        ("SM-017","مسحوق غسيل أوتوماتيك 3 كجم","Automatic Detergent 3kg",UNIT_PCS,5.60m,8.25m),
        ("SM-018","معجون أسنان بالنعناع 100 جم","Mint Toothpaste 100g",UNIT_PCS,2.00m,3.00m),
        ("SM-019","صلصة كاتشب 500 جم","Ketchup 500g",UNIT_PCS,1.55m,2.25m),
        ("SM-020","تونة خفيفة بالزيت 140 جم","Light Tuna in Oil 140g",UNIT_PCS,1.70m,2.40m)
    };
    foreach (var p in productRows)
    {
        var idto = new CreateItemDto { Code = p.Code, NameAr = p.Ar, NameEn = p.En, CategoryId = CAT_FIN, UnitId = p.Unit, CostPrice = p.Cost, SalePrice = p.Sale, MinStockLevel = 10, MaxStockLevel = 500, Barcode = "62910" + p.Code.Replace("SM-", ""), Description = p.En };
        itemIds.Add((await items.CreateAsync(idto)).Id);
        Console.WriteLine("منتج: " + p.Code + " " + p.Ar);
    }

    // --- 4) فواتير الشراء (20) - الوارد 100 وحدة لكل منتج ---
    for (int i = 0; i < itemIds.Count; i++)
    {
        var dto = new CreatePurchaseInvoiceDto
        {
            SupplierId = supplierIds[i % supplierIds.Count],
            WarehouseId = WH_MAIN,
            InvoiceType = 1,
            Note = "استلام أولي " + productRows[i].Ar,
            Lines = new List<CreatePurchaseInvoiceLineDto> { new() { ItemId = itemIds[i], Quantity = 100, UnitCost = productRows[i].Cost } }
        };
        purchaseIds.Add((await purchaseInvoices.CreateAsync(dto)).Id);
        Console.WriteLine("شراء: " + productRows[i].Code + " x 100");
    }

    // --- 5) فواتير البيع (30) ---
    for (int j = 0; j < 30; j++)
    {
        var itemIdx = j % itemIds.Count;
        var qty = 1 + (j % 3);
        var dto = new CreateSalesInvoiceDto
        {
            CustomerId = customerIds[j % customerIds.Count],
            WarehouseId = WH_MAIN,
            InvoiceType = 1,
            TaxRate = 16,
            Note = "فاتورة بيع تجزئة " + (j + 1),
            Lines = new List<CreateSalesInvoiceLineDto> { new() { ItemId = itemIds[itemIdx], Quantity = qty, UnitPrice = productRows[itemIdx].Sale } }
        };
        var inv = await salesInvoices.CreateAsync(dto);
        salesInfo.Add((inv.Id, itemIds[itemIdx], qty));
        Console.WriteLine("بيع " + (j + 1) + ": صنف " + productRows[itemIdx].Code + " x " + qty);
    }

    // --- 6) مردودات المبيعات (5) ---
    foreach (var idx in new[] { 0, 6, 12, 18, 24 })
    {
        var s = salesInfo[idx];
        var dto = new CreateSalesReturnDto
        {
            SalesInvoiceId = s.InvoiceId,
            WarehouseId = WH_MAIN,
            Note = "مردود بيع",
            Lines = new List<CreateSalesReturnLineDto> { new() { ItemId = s.ItemId, Quantity = 1, UnitPrice = 0 } }
        };
        await salesReturns.CreateAsync(dto);
        Console.WriteLine("مردود بيع: على فاتورة البيع " + (idx + 1));
    }

    // --- 7) مردودات المشتريات (3) ---
    foreach (var idx in new[] { 0, 6, 12 })
    {
        var dto = new CreatePurchaseReturnDto
        {
            PurchaseInvoiceId = purchaseIds[idx],
            WarehouseId = WH_MAIN,
            Note = "مردود شراء",
            Lines = new List<CreatePurchaseReturnLineDto> { new() { ItemId = itemIds[idx], Quantity = 5, UnitCost = 0 } }
        };
        await purchaseReturns.CreateAsync(dto);
        Console.WriteLine("مردود شراء: على فاتورة الشراء " + (idx + 1));
    }

    Console.WriteLine("=== اكتمل زرع البيانات بنجاح ===");

    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var stocks = await ctx.Items.OrderBy(i => i.Code).Select(i => new { i.Code, i.NameAr, i.CurrentStock }).ToListAsync();
    Console.WriteLine("----- رصيد المنتجات الحالي -----");
    foreach (var i in stocks) Console.WriteLine(i.Code + " " + i.NameAr + ": " + i.CurrentStock);
}