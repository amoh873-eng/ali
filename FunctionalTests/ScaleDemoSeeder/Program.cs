using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Interfaces;
using ERPSystem.Application.Services;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

// ScaleDemoSeeder — يضبط قاعدة باركود الميزان + 10 أصناف حية (لحوم/دواجن/ألبان/خضار/عسل)
// وينشئ فواتير بيع تجريبية فعّالة عبر نفس خدمات النظام (SalesInvoiceService.CreateAsync).
// الأداة تكرارية (Idempotent): تشغيلها مرة أخرى لا تكرر الأصناف أو الفواتير.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var conn = "Server=(localdb)\\mssqllocaldb;Database=ERPSystemDb;Trusted_Connection=True;TrustServerCertificate=True";
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options;
await using var db = new AppDbContext(options);
await db.Database.EnsureCreatedAsync();

// ── معرفات مرجعية (مخزن رئيسي / زبون نقدي / فئة غذائية / وحدة كغ) ──
var mainWarehouseId = Guid.Parse("30000000-0000-0000-0000-000000000001");
var cashCustomerId = Guid.Parse("90000000-0000-0000-0000-000000000001");
var foodCategoryId = Guid.Parse("2bacd992-bd45-41d5-95d2-a6721503e129");
var kgUnitId = Guid.Parse("20000000-0000-0000-0000-000000000002");

// ── 10 أصناف حية للميزان الإلكتروني (سعر/كغ) ──
var items = new (string Code, string NameAr, string NameEn, decimal Cost, decimal Sale)[]
{
    ("10001", "لحم غنم", "Lamb", 7.50m, 9.00m),
    ("10002", "لحم بقر", "Beef", 7.00m, 8.50m),
    ("10003", "دجاج طازج", "Fresh Chicken", 3.00m, 3.75m),
    ("10004", "سمك سلمون", "Salmon", 10.50m, 12.50m),
    ("10005", "طماطم", "Tomatoes", 0.60m, 0.90m),
    ("10006", "خيار", "Cucumber", 0.55m, 0.85m),
    ("10007", "بصل", "Onion", 0.40m, 0.60m),
    ("10008", "جبنة بيضاء", "White Cheese", 3.30m, 4.25m),
    ("10009", "لبنة", "Labneh", 2.80m, 3.50m),
    ("10010", "عسل جبلي", "Mountain Honey", 6.00m, 8.00m),
};

// قاعدة الميزان: البادئة 21 + كود الصنف (5) + الوزن (5، 3 عشري) + خانة تحقق.
// مثال: 21 10001 01500 8  →  كود 10001 بوزن 1.500 كغ
var ruleJson =
    "{\"Prefix\":\"21\",\"ItemCodeStart\":2,\"ItemCodeLength\":5,\"ValueStart\":7,\"ValueLength\":5,\"ValueType\":0,\"DecimalPlaces\":3}";
// ── 1) قاعدة الميزان (WeightBarcodeRuleJson) ──
var settings = await db.SystemSettings.FirstOrDefaultAsync();
var settingsDirty = false;
if (settings is null)
{
    settings = new SystemSettings { Id = SystemSettings.SingletonId };
    db.SystemSettings.Add(settings);
    settingsDirty = true;
}
if (settings.WeightBarcodeRuleJson != ruleJson)
{
    settings.WeightBarcodeRuleJson = ruleJson;
    settings.UpdatedAt = DateTime.UtcNow;
    settingsDirty = true;
}
if (settingsDirty) await db.SaveChangesAsync();
Console.WriteLine($"RULE_OK={settings.WeightBarcodeRuleJson == ruleJson}");

// ── 2) الأصناف العشرة (تنشأ فقط إذا لم تكن موجودة بكودها) ──
foreach (var (code, nameAr, nameEn, cost, sale) in items)
{
    var exists = await db.Items.AnyAsync(i => i.Code == code && !i.IsDeleted);
    if (exists)
    {
        Console.WriteLine($"ITEM_EXISTS={code}");
        continue;
    }

    var fullBarcode = MakeEan13("21" + code + "01000"); // عينة 1.000 كغ للبحث المباشر
    db.Items.Add(new Item
    {
        Id = Guid.NewGuid(),
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn,
        CategoryId = foodCategoryId,
        UnitId = kgUnitId,
        CostPrice = cost,
        SalePrice = sale,
        MinStockLevel = 5m,
        MaxStockLevel = 200m,
        Barcode = fullBarcode,
        CurrentStock = 120m, // رصيد افتتاحي للبيع التجريبي
        Description = $"ميزان إلكتروني — يباع بالوزن (كغ). باركود عينة: {fullBarcode}",
        IsActive = true,
        IsSystem = false,
        CreatedAt = DateTime.UtcNow
    });
    Console.WriteLine($"ITEM_CREATED={code}");
}
await db.SaveChangesAsync();

// ── 2b) أرصدة افتتاحية كحركات مخزون فعلية (رصيد افتتاحي +120 كغ/وحدة) ──
// CurrentStock على الصنف حقل مكرّر للوحات فقط؛ خدمات المبيعات تتحقق من الرصيد عبر
// مجموع حركات المخزون (StockMovement) للصنف+المخزن (StockAvailabilityHelper)،
// لذا يجب تسجيل حركة افتتاحية موجبة لكل صنف في المخزن الرئيسي قبل أي فاتورة بيع.
foreach (var (code, nameAr, nameEn, cost, sale) in items)
{
    var item = await db.Items.FirstOrDefaultAsync(i => i.Code == code && !i.IsDeleted);
    if (item is null) continue;

    var hasMovement = await db.StockMovements.AnyAsync(m => m.ItemId == item.Id && m.WarehouseId == mainWarehouseId);
    if (hasMovement)
    {
        Console.WriteLine($"STOCK_EXISTS={code}");
        continue;
    }

    db.StockMovements.Add(new StockMovement
    {
        Id = Guid.NewGuid(),
        ItemId = item.Id,
        WarehouseId = mainWarehouseId,
        MovementType = MovementType.OpeningBalance,
        Quantity = item.CurrentStock, // موجب = وارد (رصيد افتتاحي)
        UnitCost = item.CostPrice,
        ReferenceNumber = "SCALE-DEMO-OPEN",
        Note = "رصيد افتتاحي - ميزان إلكتروني",
        MovementDate = DateTime.Today,
        CreatedAt = DateTime.UtcNow
    });
    Console.WriteLine($"STOCK_OPENED={code} qty={item.CurrentStock}");
}
await db.SaveChangesAsync();

// ── 3) فواتير بيع تجريبية فعلية (نقدي — زبون نقدي — المخزن الرئيسي) ──
// تستخدم نفس SalesInvoiceService.CreateAsync الذي يستدعيه تطبيق الويب، فيُنشأ مخزون صادر
// وقيدان محاسبيان (بيع + تكلفة) لكل فاتورة — تظهر في لوحة التحكم والتقارير.
IJournalEntryService journal = new JournalEntryService(db);
var salesService = new SalesInvoiceService(db, journal);

var demoInvoices = new (string Code, decimal Qty)[][]
{
    new[] { ("10001", 2.500m) },                                                    // لحم غنم 2.5 كغ
    new[] { ("10003", 3.200m), ("10005", 4.000m) },                                 // دجاج + طماطم
    new[] { ("10008", 1.250m), ("10009", 0.750m) },                                 // جبنة + لبنة
    new[] { ("10010", 0.500m), ("10004", 1.800m) },                                 // عسل + سلمون
    new[] { ("10002", 3.000m), ("10006", 2.500m), ("10007", 1.500m) },              // لحم + خيار + بصل
};

// تجنّب التكرار: نتخطّى إنشاء فواتير العرض إذا وُجدت سابقاً
var existingDemo = await db.SalesInvoices.AnyAsync(i => i.Note != null && i.Note.StartsWith("SCALE-DEMO"));
if (existingDemo)
{
    Console.WriteLine("SALES_SKIP=already-seeded");
}
else
{
    foreach (var demo in demoInvoices)
    {
        var lines = new List<CreateSalesInvoiceLineDto>();
        foreach (var (code, qty) in demo)
        {
            var item = await db.Items.FirstAsync(i => i.Code == code && !i.IsDeleted);
            lines.Add(new CreateSalesInvoiceLineDto { ItemId = item.Id, Quantity = qty, UnitPrice = item.SalePrice });
        }

        var invoice = await salesService.CreateAsync(new CreateSalesInvoiceDto
        {
            CustomerId = cashCustomerId,
            WarehouseId = mainWarehouseId,
            InvoiceDate = DateTime.Today,
            InvoiceType = 1, // نقدي
            DiscountPercentage = 0m,
            TaxRate = 0m,
            Note = "SCALE-DEMO",
            IsPos = true,
            Lines = lines
        });
        Console.WriteLine($"SALE_CREATED={invoice.InvoiceNumber} items={lines.Count} qtySum={lines.Sum(l => l.Quantity):F3} total={invoice.TotalAmount:N2}");
    }
}

Console.WriteLine("DONE_SEEDER");

// ── دالة حساب خانة التحقق EAN-13 ──
static string MakeEan13(string base12)
{
    if (base12.Length != 12 || !base12.All(char.IsDigit))
        throw new ArgumentException("يتطلب 12 رقماً فقط");
    var sum = 0;
    for (var i = 0; i < base12.Length; i++)
    {
        var d = base12[i] - '0';
        sum += (i % 2 == 0) ? d : d * 3;
    }
    var check = (10 - (sum % 10)) % 10;
    return base12 + check;
}