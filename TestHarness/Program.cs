using System.Text;
using ERPSystem.Application.DTOs.Categories;
using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.DTOs.Departments;
using ERPSystem.Application.DTOs.Employees;
using ERPSystem.Application.DTOs.Items;
using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.DTOs.Leaves;
using ERPSystem.Application.DTOs.Movements;
using ERPSystem.Application.DTOs.Positions;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.DTOs.Units;
using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Application.Interfaces;
using ERPSystem.Application.Services;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = Encoding.UTF8;

var connStr = "Server=(localdb)\\mssqllocaldb;Database=ERPSystemDb;Trusted_Connection=True;TrustServerCertificate=True";
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connStr).Options;
using var db = new AppDbContext(options);
await db.Database.MigrateAsync();

// ===================== الخدمات =====================
IJournalEntryService journal = new JournalEntryService(db);
IItemService itemService = new ItemService(db);
ICategoryService categoryService = new CategoryService(db);
IUnitService unitService = new UnitService(db);
IWarehouseService warehouseService = new WarehouseService(db);
ICustomerService customerService = new CustomerService(db);
ISupplierService supplierService = new SupplierService(db);
ISalesInvoiceService salesInvoiceService = new SalesInvoiceService(db, journal);
IPurchaseInvoiceService purchaseInvoiceService = new PurchaseInvoiceService(db, journal);
ISalesReturnService salesReturnService = new SalesReturnService(db, journal);
IPurchaseReturnService purchaseReturnService = new PurchaseReturnService(db, journal);
ISalesQuoteService salesQuoteService = new SalesQuoteService(db, salesInvoiceService);
IPurchaseOrderService purchaseOrderService = new PurchaseOrderService(db, purchaseInvoiceService);
IStockMovementService stockMovementService = new StockMovementService(db);
IStockCountService stockCountService = new StockCountService(db);
IReportService reportService = new ReportService(db);
IDepartmentService departmentService = new DepartmentService(db);
IPositionService positionService = new PositionService(db);
IEmployeeService employeeService = new EmployeeService(db);
ILeaveService leaveService = new LeaveService(db);

// ===================== أدوات مساعدة =====================
int pass = 0, fail = 0;
void P(string s = "") => Console.WriteLine(s);
void Sep(string t) { P(); P("======== " + t + " ========"); }
void Check(string name, bool ok, string detail = "")
{
    if (ok) { pass++; P($"  [PASS] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" — {detail}")); }
    else { fail++; P($"  [FAIL] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" — {detail}")); }
}

var ts = DateTime.Now.ToString("HHmmss");

// ===================== 1) البيانات الأساسية =====================
Sep("1) البيانات الأساسية (وحدات/فئات/مخازن)");
var units = await unitService.GetAllAsync();
var categories = await categoryService.GetAllAsync();
var warehouses = await warehouseService.GetAllAsync();
Check("وحدات القياس جاهزة", units.Count > 0, $"{units.Count} وحدة");
Check("فئات الأصناف جاهزة", categories.Count > 0, $"{categories.Count} فئة");
Check("المخازن جاهزة", warehouses.Count > 0, $"{warehouses.Count} مخزن");
var wh = warehouses.First();
var wh2 = warehouses.Skip(1).FirstOrDefault();
if (wh2 is null)
{
    var newWh = await warehouseService.CreateAsync(new CreateWarehouseDto
    {
        Code = $"WH-{ts}", NameAr = "مخزن اختبار ثانٍ"
    });
    wh2 = newWh;
    Check("إنشاء مخزن ثانٍ (للتحويل)", true, $"{newWh.Code} = {newWh.NameAr}");
}
var unitPcs = units.First(u => u.Code == "PCS");
var catRaw = categories.First(c => c.Code == "RAW");

// ===================== 2) أصناف جديدة =====================
Sep("2) إضافة أصناف جديدة");
var item1 = await itemService.CreateAsync(new CreateItemDto
{
    Code = $"ITM-{ts}-1", NameAr = "صنف اختبار 1", CategoryId = catRaw.Id, UnitId = unitPcs.Id,
    CostPrice = 10m, SalePrice = 15m, MinStockLevel = 5, MaxStockLevel = 100
});
var item2 = await itemService.CreateAsync(new CreateItemDto
{
    Code = $"ITM-{ts}-2", NameAr = "صنف اختبار 2 (أسعار عشرية)", CategoryId = catRaw.Id, UnitId = unitPcs.Id,
    CostPrice = 20.50m, SalePrice = 30.75m, MinStockLevel = 3, MaxStockLevel = 50
});
Check("إنشاء صنف 1", item1.Id != Guid.Empty, $"{item1.Code} = {item1.NameAr}");
Check("إنشاء صنف 2 بأسعار عشرية", item2.Id != Guid.Empty, $"سعر البيع = {item2.SalePrice}");

// ===================== 3) عملاء وموردون جدد =====================
Sep("3) إضافة عملاء وموردين جدد");
var customer = await customerService.CreateAsync(new CreateCustomerDto
{
    Code = $"CUS-{ts}", NameAr = "عميل اختبار", Phone = "0790000000", CreditLimit = 10000
});
var supplier = await supplierService.CreateAsync(new CreateSupplierDto
{
    Code = $"SUP-{ts}", NameAr = "مورد اختبار", Phone = "0780000000"
});
Check("إنشاء عميل", customer.Id != Guid.Empty, $"{customer.Code} = {customer.NameAr}");
Check("إنشاء مورد", supplier.Id != Guid.Empty, $"{supplier.Code} = {supplier.NameAr}");

// ===================== 4) أمر شراء → فاتورة مشتريات =====================
Sep("4) أمر شراء → اعتماد → تحويل إلى فاتورة مشتريات");
var po = await purchaseOrderService.CreateAsync(new CreatePurchaseOrderDto
{
    SupplierId = supplier.Id, OrderDate = DateTime.Today,
    DiscountPercentage = 0, TaxRate = 16,
    Note = "أمر شراء اختبار شامل",
    Lines = new List<CreatePurchaseOrderLineDto>
    {
        new() { ItemId = item1.Id, Quantity = 50, UnitPrice = 10m },
        new() { ItemId = item2.Id, Quantity = 30, UnitPrice = 20.50m }
    }
});
Check("إنشاء أمر شراء", po.Id != Guid.Empty, $"رقم {po.OrderNumber}");
await purchaseOrderService.ApproveAsync(po.Id);
Check("اعتماد أمر الشراء", true, "تم الاعتماد");
await purchaseOrderService.ConvertToInvoiceAsync(po.Id, wh.Id, 2);
Check("تحويل الأمر إلى فاتورة مشتريات", true, "تم التحويل والترحيل");
var item1AfterPO = (await itemService.GetAllAsync()).First(i => i.Id == item1.Id);
Check("زيادة مخزون الصنف 1 بعد الشراء", item1AfterPO.CurrentStock == 50, $"الرصيد = {item1AfterPO.CurrentStock}");

var pi = await purchaseInvoiceService.CreateAsync(new CreatePurchaseInvoiceDto
{
    SupplierId = supplier.Id, WarehouseId = wh.Id, InvoiceDate = DateTime.Today, InvoiceType = 1,
    Note = "فاتورة مشتريات مباشرة (اختبار)",
    Lines = new List<CreatePurchaseInvoiceLineDto>
    {
        new() { ItemId = item1.Id, Quantity = 20, UnitCost = 9.50m }
    }
});
Check("إنشاء فاتورة مشتريات مباشرة", pi.InvoiceNumber != null, $"فاتورة {pi.InvoiceNumber}");

// ===================== 5) عرض سعر → فاتورة مبيعات =====================
Sep("5) عرض سعر → اعتماد → تحويل إلى فاتورة مبيعات");
var quote = await salesQuoteService.CreateAsync(new CreateSalesQuoteDto
{
    CustomerId = customer.Id, QuoteDate = DateTime.Today,
    DiscountPercentage = 5, TaxRate = 16,
    Note = "عرض سعر اختبار شامل",
    Lines = new List<CreateSalesQuoteLineDto>
    {
        new() { ItemId = item1.Id, Quantity = 10, UnitPrice = 15m },
        new() { ItemId = item2.Id, Quantity = 5, UnitPrice = 30.75m }
    }
});
Check("إنشاء عرض سعر", quote.Id != Guid.Empty, $"رقم {quote.QuoteNumber}");
await salesQuoteService.ApproveAsync(quote.Id);
Check("اعتماد عرض السعر", true, "تم الاعتماد");
await salesQuoteService.ConvertToInvoiceAsync(quote.Id, wh.Id, 1);
var item1AfterSale = (await itemService.GetAllAsync()).First(i => i.Id == item1.Id);
Check("نقص مخزون الصنف 1 بعد البيع", item1AfterSale.CurrentStock == 60, $"الرصيد = {item1AfterSale.CurrentStock}");

var si = await salesInvoiceService.CreateAsync(new CreateSalesInvoiceDto
{
    CustomerId = customer.Id, WarehouseId = wh.Id, InvoiceDate = DateTime.Today, InvoiceType = 2,
    DiscountPercentage = 0, TaxRate = 16,
    Note = "فاتورة مبيعات مباشرة (اختبار)",
    Lines = new List<CreateSalesInvoiceLineDto>
    {
        new() { ItemId = item1.Id, Quantity = 5, UnitPrice = 15m },
        new() { ItemId = item2.Id, Quantity = 3, UnitPrice = 30.75m }
    }
});
Check("إنشاء فاتورة مبيعات مباشرة", si.InvoiceNumber != null, $"فاتورة {si.InvoiceNumber}");

// ===================== 6) المردودات =====================
Sep("6) مردود مبيعات + مردود مشتريات");
var sr = await salesReturnService.CreateAsync(new CreateSalesReturnDto
{
    SalesInvoiceId = si.Id, WarehouseId = wh.Id, ReturnDate = DateTime.Today,
    Note = "مردود مبيعات اختبار",
    Lines = new List<CreateSalesReturnLineDto>
    {
        new() { ItemId = item1.Id, Quantity = 2, UnitPrice = 15m }
    }
});
Check("إنشاء مردود مبيعات", sr.ReturnNumber != null, $"رقم {sr.ReturnNumber}");

var pr = await purchaseReturnService.CreateAsync(new CreatePurchaseReturnDto
{
    PurchaseInvoiceId = pi.Id, WarehouseId = wh.Id, ReturnDate = DateTime.Today,
    Note = "مردود مشتريات اختبار",
    Lines = new List<CreatePurchaseReturnLineDto>
    {
        new() { ItemId = item1.Id, Quantity = 3, UnitCost = 9.50m }
    }
});
Check("إنشاء مردود مشتريات", pr.ReturnNumber != null, $"رقم {pr.ReturnNumber}");

// ===================== 7) حركات المخزون =====================
Sep("7) حركات المخزون (إضافة/خصم/تحويل/جرد)");
await stockMovementService.CreateAsync(new CreateStockMovementDto
{
    ItemId = item1.Id, WarehouseId = wh.Id, MovementType = (int)MovementType.AdjustmentIn,
    Quantity = 20, UnitCost = 10m, ReferenceNumber = "ADJ-IN", Note = "تسوية إضافة"
});
Check("حركة إضافة (تسوية)", true, "تم");

await stockMovementService.CreateAsync(new CreateStockMovementDto
{
    ItemId = item1.Id, WarehouseId = wh.Id, MovementType = (int)MovementType.AdjustmentOut,
    Quantity = 5, UnitCost = 10m, ReferenceNumber = "ADJ-OUT", Note = "تسوية خصم"
});
Check("حركة خصم (تسوية)", true, "تم");

if (wh2.Id != wh.Id)
{
    await stockMovementService.CreateTransferAsync(new TransferStockDto
    {
        ItemId = item1.Id, SourceWarehouseId = wh.Id, DestinationWarehouseId = wh2.Id,
        Quantity = 8, ReferenceNumber = "TRF-1"
    });
    Check("تحويل بين المخازن", true, $"{wh.Code} → {wh2.Code}");
}
else
{
    Check("تحويل بين المخازن", false, "لا يوجد مخزن ثانٍ");
}

var balances = await stockMovementService.GetStockBalancesAsync();
Check("أرصدة المخزون محسوبة", balances.Count > 0, $"{balances.Count} رصيد");

var sc = await stockCountService.CreateAsync(new CreateStockCountDto
{
    WarehouseId = wh.Id, CountDate = DateTime.Today, Note = "جرد اختبار",
    Lines = new List<StockCountLineInput>
    {
        new() { ItemId = item1.Id, CountedQuantity = 100 }
    }
});
Check("إنشاء جرد دوري", sc.StockCountNumber != null, $"رقم {sc.StockCountNumber}");

// ===================== 8) قيد محاسبي يدوي =====================
Sep("8) قيد محاسبي يدوي");
var cashAccount = await db.Set<Account>().FirstAsync(a => a.Code == "1100");
var revenueAccount = await db.Set<Account>().FirstAsync(a => a.Code == "4100");
var manualEntry = await journal.CreateManualEntryAsync(DateTime.Today, "MAN-1", "قيد يدوي اختبار",
    new List<JournalEntryLegInput>
    {
        new() { AccountId = cashAccount.Id, DebitAmount = 500, CreditAmount = 0 },
        new() { AccountId = revenueAccount.Id, DebitAmount = 0, CreditAmount = 500 }
    });
Check("إنشاء قيد يدوي متوازن", manualEntry.EntryNumber != null, $"قيد {manualEntry.EntryNumber}");

// ===================== 9) الموارد البشرية =====================
Sep("9) الموارد البشرية (قسم/مسمى/موظف/إجازة)");
var dept = await departmentService.CreateAsync(new CreateDepartmentDto
{
    Code = $"DEP-{ts}", NameAr = "قسم اختبار"
});
Check("إنشاء قسم", dept.Id != Guid.Empty, dept.NameAr);

var pos = await positionService.CreateAsync(new CreatePositionDto
{
    Code = $"POS-{ts}", NameAr = "مسمى اختبار", DepartmentId = dept.Id
});
Check("إنشاء مسمى وظيفي", pos.Id != Guid.Empty, pos.NameAr);

var emp = await employeeService.CreateAsync(new CreateEmployeeDto
{
    EmployeeNumber = $"EMP-{ts}", NameAr = "موظف اختبار", DepartmentId = dept.Id, PositionId = pos.Id,
    HireDate = DateTime.Today, BasicSalary = 1500.50m, Status = EmployeeStatus.Active
});
Check("إنشاء موظف", emp.Id != Guid.Empty, $"{emp.EmployeeNumber} = {emp.NameAr}");

var leave = await leaveService.CreateAsync(new CreateLeaveDto
{
    EmployeeId = emp.Id, LeaveType = LeaveType.Annual,
    StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(5), Reason = "إجازة اختبار"
});
Check("إنشاء طلب إجازة", leave.Id != Guid.Empty, $"الحالة = {leave.Status}");
await leaveService.SetStatusAsync(leave.Id, LeaveStatus.Approved);
Check("الموافقة على الإجازة", true, "تمت الموافقة");

// ===================== 10) التقارير المالية =====================
Sep("10) التقارير المالية");
var tb = await reportService.GetTrialBalanceAsync();
Check("ميزان المراجعة متوازن", tb.IsBalanced, $"مدين {tb.TotalDebit:N2} / دائن {tb.TotalCredit:N2}");

var income = await reportService.GetIncomeStatementAsync(DateTime.Today.AddDays(-30), DateTime.Today);
Check("قائمة الدخل", income.TotalRevenue >= 0, $"إيرادات {income.TotalRevenue:N2} / صافي {income.NetIncome:N2}");

var bs = await reportService.GetBalanceSheetAsync(DateTime.Today);
Check("الميزانية العمومية متوازنة", bs.IsBalanced, $"أصول {bs.TotalAssets:N2} / خصوم+حقوق {bs.TotalLiabilitiesAndEquity:N2}");

var inventoryAccount = await db.Set<Account>().FirstAsync(a => a.Code == "1300");
var ledger = await reportService.GetAccountLedgerAsync(inventoryAccount.Id, DateTime.Today.AddDays(-30), DateTime.Today);
Check("دفتر أستاذ المخزون", true, $"افتتاحي {ledger.OpeningBalance:N2} → ختامي {ledger.ClosingBalance:N2}");

// ===================== الملخص =====================
Sep("ملخص الاختبار الشامل");
P($"إجمالي الفحوصات: {pass + fail} | ناجح: {pass} | فاشل: {fail}");
P(fail == 0 ? "✅ كل الفحوصات نجحت بنجاح" : "❌ توجد فحوصات فاشلة — راجع الأخطاء أعلاه");



