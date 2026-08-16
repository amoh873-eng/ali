using System.Data;
using System.Text;
using ERPSystem.Application.DTOs.Crm;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.DTOs.Sales;
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

IJournalEntryService journal = new JournalEntryService(db);
IExpenseCategoryService expenseCategoryService = new ExpenseCategoryService(db);
IExpenseService expenseService = new ExpenseService(db, journal);
ILeadService leadService = new LeadService(db);
ISalesInvoiceService salesInvoiceService = new SalesInvoiceService(db, journal);

int pass = 0, fail = 0;
void Check(string name, bool ok, string detail = "")
{
    if (ok) { pass++; Console.WriteLine($"  [PASS] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" — {detail}")); }
    else { fail++; Console.WriteLine($"  [FAIL] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" — {detail}")); }
}
void Sep(string t) { Console.WriteLine(); Console.WriteLine("======== " + t + " ========"); }

var ts = DateTime.Now.ToString("HHmmss");

// ===================== 1) المصاريف =====================
Sep("1) المصاريف: فئة + سند + قيد محاسبي تلقائي");

var expenseRoot = await db.Set<Account>().FirstAsync(a => a.Code == "5");
var cashAccount = await db.Set<Account>().FirstAsync(a => a.Code == "1100");

var expenseAccount = new Account
{
    Id = Guid.NewGuid(),
    Code = $"52{ts}",
    NameAr = "مصروف اختبار",
    NameEn = "Test Expense",
    AccountType = AccountType.Expense,
    NormalBalance = NormalBalance.Debit,
    ParentAccountId = expenseRoot.Id,
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};
db.Set<Account>().Add(expenseAccount);
await db.SaveChangesAsync();
Check("إنشاء حساب مصروف", expenseAccount.Id != Guid.Empty, expenseAccount.Code);

var expenseCategory = await expenseCategoryService.CreateAsync(new CreateExpenseCategoryDto
{
    Code = $"EC-{ts}", NameAr = "فئة مصروف اختبار", NameEn = "Test expense category", AccountId = expenseAccount.Id
});
Check("إنشاء فئة مصروف", expenseCategory.Id != Guid.Empty, expenseCategory.Code);

var cashBefore = await db.Set<Account>().Where(a => a.Code == "1100").Select(a => a.CurrentBalance).FirstAsync();
var expenseEntry = await expenseService.CreateAsync(new CreateExpenseEntryDto
{
    ExpenseCategoryId = expenseCategory.Id, ExpenseDate = DateTime.Today, Amount = 350m, PaymentMethod = (int)PaymentMethod.Cash
});
Check("إنشاء سند مصروف", !string.IsNullOrEmpty(expenseEntry.EntryNumber), expenseEntry.EntryNumber);

var expenseJe = await db.Set<JournalEntry>().Include(e => e.Lines).FirstAsync(e => e.Id == expenseEntry.JournalEntryId);
var totalDebit = expenseJe.Lines.Sum(l => l.DebitAmount);
var totalCredit = expenseJe.Lines.Sum(l => l.CreditAmount);
Check("قيد المصروف متوازن", totalDebit == totalCredit && totalDebit == 350m, $"مدين {totalDebit:N2} / دائن {totalCredit:N2}");
Check("مدين حساب المصروف", expenseJe.Lines.Any(l => l.AccountId == expenseAccount.Id && l.DebitAmount == 350m), "حساب المصروف مدين");
Check("دائن الصندوق", expenseJe.Lines.Any(l => l.AccountId == cashAccount.Id && l.CreditAmount == 350m), "الصندوق دائن");

var cashAfter = await db.Set<Account>().Where(a => a.Code == "1100").Select(a => a.CurrentBalance).FirstAsync();
Check("انخفاض رصيد الصندوق", cashAfter == cashBefore - 350m, $"{cashBefore:N2} → {cashAfter:N2}");

// ===================== 2) CRM: تحويل محتمل لعميل =====================
Sep("2) CRM: تحويل عميل محتمل إلى عميل فعلي");

var lead = await leadService.CreateAsync(new CreateLeadDto
{
    NameAr = $"عميل محتمل {ts}", NameEn = "Test lead", Phone = "0599000000", Source = (int)LeadSource.Website
});
Check("إنشاء عميل محتمل", !string.IsNullOrEmpty(lead.Code), lead.Code);

var converted = await leadService.ConvertToCustomerAsync(new ConvertLeadDto
{
    LeadId = lead.Id, NameAr = lead.NameAr, NameEn = lead.NameEn, Phone = lead.Phone
});
Check("تحويل المحتمل لعميل", converted.Status == (int)LeadStatus.Converted && converted.ConvertedCustomerId.HasValue,
    $"الحالة=محوّل، العميل={converted.ConvertedCustomerName}");

var linkedCustomer = await db.Set<Customer>().FirstAsync(c => c.Id == converted.ConvertedCustomerId);
Check("إنشاء العميل الفعلي", !string.IsNullOrEmpty(linkedCustomer.Code), linkedCustomer.Code);

// ===================== 3) POS: بيع نقدي (IsPos) + قيد + حركة مخزون =====================
Sep("3) POS: بيع نقدي مميّز IsPos وربط المحوَّل لعميل بفاتورة");

var walkIn = await db.Set<Customer>().FirstAsync(c => c.Code == "CASH");
var wh = await db.Set<Warehouse>().FirstAsync(w => w.IsDefault);
var category = await db.Set<Category>().FirstAsync();
var unit = await db.Set<Unit>().FirstAsync();

var posItem = new Item
{
    Id = Guid.NewGuid(), Code = $"POSITEM{ts}", NameAr = "صنف نقطة بيع", NameEn = "POS test item",
    CategoryId = category.Id, UnitId = unit.Id, CostPrice = 50m, SalePrice = 100m,
    MinStockLevel = 0, MaxStockLevel = 1000, IsActive = true, CurrentStock = 50, CreatedAt = DateTime.UtcNow
};
db.Set<Item>().Add(posItem);
db.Set<StockMovement>().Add(new StockMovement
{
    Id = Guid.NewGuid(), ItemId = posItem.Id, WarehouseId = wh.Id, MovementType = MovementType.OpeningBalance,
    Quantity = 50, UnitCost = 50m, ReferenceNumber = $"OPEN-{ts}", Note = "رصيد افتتاحي اختبار",
    MovementDate = DateTime.Today, CreatedAt = DateTime.UtcNow
});
await db.SaveChangesAsync();
Check("تجهيز صنف برصيد 50", true, posItem.Code);

var posInvoice = await salesInvoiceService.CreateAsync(new CreateSalesInvoiceDto
{
    CustomerId = walkIn.Id, WarehouseId = wh.Id, InvoiceDate = DateTime.Today,
    InvoiceType = (int)SalesInvoiceType.Cash, DiscountPercentage = 0, TaxRate = 16, Note = "POS test", IsPos = true,
    Lines = new List<CreateSalesInvoiceLineDto> { new() { ItemId = posItem.Id, Quantity = 3, UnitPrice = posItem.SalePrice } }
});
Check("إنشاء فاتورة POS (زبون نقدي)", !string.IsNullOrEmpty(posInvoice.InvoiceNumber), $"{posInvoice.InvoiceNumber}");
Check("الفاتورة مميّزة IsPos", posInvoice.IsPos, "IsPos=true");
Check("إجمالي الفاتورة (3 × 100 + ضريبة 16%)", posInvoice.TotalAmount == 348m, $"الإجمالي = {posInvoice.TotalAmount:N2}");

var posStock = await db.Set<StockMovement>().Where(m => m.ItemId == posItem.Id && m.WarehouseId == wh.Id).SumAsync(m => (decimal?)m.Quantity) ?? 0m;
Check("انخفاض المخزون بمقدار 3", posStock == 47m, $"الرصيد = {posStock}");

var linkedPosInvoice = await salesInvoiceService.CreateAsync(new CreateSalesInvoiceDto
{
    CustomerId = linkedCustomer.Id, WarehouseId = wh.Id, InvoiceDate = DateTime.Today,
    InvoiceType = (int)SalesInvoiceType.Cash, DiscountPercentage = 0, TaxRate = 16, Note = "POS from lead", IsPos = true,
    Lines = new List<CreateSalesInvoiceLineDto> { new() { ItemId = posItem.Id, Quantity = 1, UnitPrice = posItem.SalePrice } }
});
Check("ربط المحوَّل لعميل بفاتورة POS", linkedPosInvoice.CustomerId == linkedCustomer.Id && linkedPosInvoice.IsPos,
    $"{linkedPosInvoice.InvoiceNumber} → العميل {linkedCustomer.Code}");

// ===================== 4) الصلاحيات: أذونات دور Admin =====================
Sep("4) الصلاحيات: أذونات دور Admin (44 = 11 موديول × 4 أفعال)");

var conn = db.Database.GetDbConnection();
if (conn.State != ConnectionState.Open) await conn.OpenAsync();
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(*) FROM AspNetRoleClaims rc JOIN AspNetRoles r ON rc.RoleId = r.Id WHERE r.Name = 'Admin' AND rc.ClaimType = 'Permission'";
    var adminPermCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    Check("دور Admin يحمل كل الأذونات", adminPermCount == 44, $"{adminPermCount} إذنًا");
}

// ===================== الملخص =====================
Sep("ملخص اختبار الموديولات الجديدة");
Console.WriteLine($"إجمالي الفحوصات: {pass + fail} | ناجح: {pass} | فاشل: {fail}");
Console.WriteLine(fail == 0 ? "✅ كل فحوصات الموديولات الجديدة نجحت" : "❌ توجد فحوصات فاشلة — راجع الأخطاء أعلاه");

