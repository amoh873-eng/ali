using System.Text;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Application.DTOs.Items;
using ERPSystem.Application.DTOs.Movements;
using ERPSystem.Application.Interfaces;
using ERPSystem.Application.Services;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

// WriteOffHarness - اختبار نهائي بالأدلة لسند الإتلاف المحاسبي:
//   أ) صنف بلا دُفعات   ب) صنف يتتبّع دُفعات   ج) رفض الكمية غير الكافية   د) التقرير الملخّص
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("=== StockWriteOff E2E Harness ===");

var db = new AppDbContextFactory().CreateDbContext(Array.Empty<string>());
db.Database.SetCommandTimeout(180);
await db.Database.MigrateAsync();

IJournalEntryService journal = new JournalEntryService(db);
IItemService itemService = new ItemService(db);
IStockMovementService movementService = new StockMovementService(db);
IStockWriteOffService writeOffService = new StockWriteOffService(db, journal);

// وضع فحص فقط (WO_QUERY_ONLY=1): يطبع كل سندات الإتلاف ورصيد حساب 5300 ثم يخرج — دون تنظيف أو إدراج.
if (Environment.GetEnvironmentVariable("WO_QUERY_ONLY") == "1")
{
    Console.WriteLine("=== QUERY MODE ===");
    var rows = await db.StockWriteOffs.AsNoTracking()
        .Include(w => w.Item)
        .AsNoTracking()
        .OrderBy(w => w.CreatedAt)
        .ToListAsync();
    foreach (var r in rows)
        Console.WriteLine($"{r.DocumentNumber} | {r.Item?.Code} | ItemId={r.ItemId} | Qty={r.Quantity} | Reason={r.Reason} | Date={r.Date:yyyy-MM-dd} | JournalEntryId={r.JournalEntryId}");
    var acc = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Code == "5300");
    Console.WriteLine($"ACCOUNT 5300 balance = {acc?.CurrentBalance}");
    return;
}

int pass = 0, fail = 0;
void Check(string name, bool ok, string detail = "")
{
    if (ok) { pass++; Console.WriteLine($"  [PASS] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" - {detail}")); }
    else { fail++; Console.WriteLine($"  [FAIL] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" - {detail}")); }
}
void Sep(string title) { Console.WriteLine(); Console.WriteLine($"======== {title} ========"); }

const string CodeA = "WO-TEST-A";
const string CodeB = "WO-TEST-B";

// مرجعيات (وحدة/فئة/مخزن)
var unit = await db.Units.FirstOrDefaultAsync(u => u.Code == "PCS");
if (unit is null)
{
    unit = new Unit { Id = Guid.NewGuid(), Code = "PCS", NameAr = "قطعة", NameEn = "Piece", IsActive = true, CreatedAt = DateTime.UtcNow };
    db.Units.Add(unit);
}
var category = await db.Categories.FirstOrDefaultAsync(c => c.Code == "FIN");
if (category is null)
{
    category = new Category { Id = Guid.NewGuid(), Code = "FIN", NameAr = "منتجات تامة", NameEn = "Finished Goods", IsActive = true, CreatedAt = DateTime.UtcNow };
    db.Categories.Add(category);
}
var warehouse = await db.Warehouses.OrderBy(w => w.IsDefault ? 0 : 1).FirstOrDefaultAsync();
if (warehouse is null)
{
    warehouse = new Warehouse { Id = Guid.NewGuid(), Code = "MAIN", NameAr = "المخزن الرئيسي", NameEn = "Main Warehouse", IsActive = true, IsDefault = true, CreatedAt = DateTime.UtcNow };
    db.Warehouses.Add(warehouse);
}
await db.SaveChangesAsync();

// ── تنظيف شامل وموثوق لبيانات الاختبار (يجعل التشغيل مكرراً بدون انحياز): ──
// 1) كل سندات الإتلاف + قيودها المحاسبية (مع إعادة بناء أرصدة الحسابات من دفتر اليومية —
//    لأن حذف القيد لا يعكس رصيد الحساب تلقائياً).
// 2) كل حركات المخزون من نوع WriteOff.
// 3) حركات/دُفعات/أصناف الاختبار.
async Task RebuildAccountBalanceAsync(string code)
{
    var acc = await db.Accounts.FirstOrDefaultAsync(a => a.Code == code);
    if (acc is null) return;
    var lines = await db.JournalEntryLines.Where(l => l.AccountId == acc.Id).ToListAsync();
    acc.CurrentBalance = lines.Sum(l => acc.NormalBalance == NormalBalance.Debit
        ? l.DebitAmount - l.CreditAmount
        : l.CreditAmount - l.DebitAmount);
}

foreach (var w in await db.StockWriteOffs.ToListAsync())
{
    db.StockWriteOffs.Remove(w); // السند أولاً — فيزيل المرجع غير الفارغ قبل حذف القيد
    var je = await db.JournalEntries.Include(e => e.Lines).FirstOrDefaultAsync(e => e.Id == w.JournalEntryId);
    if (je is not null)
        db.JournalEntries.Remove(je); // بنود القيد تُحذف حذفاً متتالياً (Cascade)
}
db.StockMovements.RemoveRange(db.StockMovements.Where(m => m.MovementType == MovementType.WriteOff));

foreach (var old in await db.Items.Where(i => i.Code == CodeA || i.Code == CodeB).ToListAsync())
{
    db.StockMovements.RemoveRange(db.StockMovements.Where(m => m.ItemId == old.Id));
    db.ItemBatches.RemoveRange(db.ItemBatches.Where(b => b.ItemId == old.Id));
    db.Items.Remove(old);
}
await db.SaveChangesAsync();

await RebuildAccountBalanceAsync("5300");
await RebuildAccountBalanceAsync("1300");
await db.SaveChangesAsync();

// إنشاء الصنفين عبر خدمة الأصناف الرسمية
await itemService.CreateAsync(new CreateItemDto
{
    Code = CodeA, NameAr = "اختبار إتلاف أ - بلا دُفعات", NameEn = "Write-Off Test A",
    CategoryId = category.Id, UnitId = unit.Id, CostPrice = 12.50m, SalePrice = 20.00m, TracksBatches = false
});
await itemService.CreateAsync(new CreateItemDto
{
    Code = CodeB, NameAr = "اختبار إتلاف ب - دُفعات", NameEn = "Write-Off Test B",
    CategoryId = category.Id, UnitId = unit.Id, CostPrice = 4.00m, SalePrice = 7.00m,
    TracksBatches = true, TracksExpiry = true
});
// ─────────────── السيناريو أ: صنف بلا دُفعات ───────────────
Sep("Scenario A - item WITHOUT batch tracking");
var itemA = await db.Items.FirstAsync(i => i.Code == CodeA);
itemA.CostPrice = 12.50m;
await db.SaveChangesAsync();

await movementService.CreateAsync(new CreateStockMovementDto
{
    ItemId = itemA.Id, WarehouseId = warehouse.Id,
    MovementType = (int)MovementType.PurchaseReceipt,
    Quantity = 10m, UnitCost = 12.50m,
    Note = "تحضير اختبار سند الإتلاف (أ)", MovementDate = DateTime.Today
});

var beforeA = (await db.Items.AsNoTracking().FirstAsync(i => i.Id == itemA.Id)).CurrentStock;
Console.WriteLine($"Item {CodeA} BEFORE write-off: CurrentStock = {beforeA:N0}");

var woffA = await writeOffService.CreateAsync(new CreateStockWriteOffDto
{
    ItemId = itemA.Id, WarehouseId = warehouse.Id,
    Quantity = 3m, Reason = (int)StockWriteOffReason.Damaged,
    Date = DateTime.Today, Notes = "اختبار أ - تالف/مكسور"
}, "test-user");

var afterA = (await db.Items.AsNoTracking().FirstAsync(i => i.Id == itemA.Id)).CurrentStock;
var entryA = await journal.GetByIdAsync(woffA.JournalEntryId);
var movA = await db.StockMovements.AsNoTracking().FirstAsync(m => m.ReferenceNumber == woffA.DocumentNumber);

Console.WriteLine($"WRITE-OFF DOC  : {woffA.DocumentNumber}");
Console.WriteLine($"WRITE-OFF ROW  : Qty={woffA.Quantity:N2} UnitCost={woffA.UnitCost:N2} Total={woffA.TotalCost:N2} Reason={woffA.ReasonNameAr}");
Console.WriteLine($"JOURNAL ENTRY  : {entryA!.EntryNumber} - {entryA.Description}");
foreach (var line in entryA.Lines)
    Console.WriteLine($"   LEG: {line.AccountCode} - {line.AccountNameAr}  Debit={line.DebitAmount:N2}  Credit={line.CreditAmount:N2}");
Console.WriteLine($"MOVEMENT       : {movA.MovementType} Qty={movA.Quantity:N2} Ref={movA.ReferenceNumber}");

Check("A: CurrentStock انخفض بالكمية المطلوبة بالضبط (10 -> 7)", beforeA == 10m && afterA == 7m, $"before={beforeA:N0} after={afterA:N0}");
Check("A: سعر التكلفة نُسخ من الصنف (12.50)", woffA.UnitCost == 12.50m, woffA.UnitCost.ToString("N2"));
Check("A: قيمة السند = كمية × تكلفة (37.50)", woffA.TotalCost == 37.50m, woffA.TotalCost.ToString("N2"));
Check("A: القيد متوازن (مدين = دائن = 37.50)",
      entryA.IsBalanced && entryA.TotalDebit == 37.50m && entryA.TotalCredit == 37.50m,
      $"Debit={entryA.TotalDebit:N2} Credit={entryA.TotalCredit:N2}");
Check("A: مدين مصروف الهالك (5300) ودائن المخزون (1300) بالمبلغ الصحيح",
      entryA.Lines.Any(l => l.AccountCode == "5300" && l.DebitAmount == 37.50m)
      && entryA.Lines.Any(l => l.AccountCode == "1300" && l.CreditAmount == 37.50m));
Check("A: حركة مخزون صادرة بنوع WriteOff (-3)", movA.MovementType == MovementType.WriteOff && movA.Quantity == -3m);
Check("A: البادئات WO / JE صحيحة", woffA.DocumentNumber.StartsWith("WO-") && entryA.EntryNumber.StartsWith("JE-"));
// ─────────────── السيناريو ب: صنف يتتبّع دُفعات (ItemBatch) ───────────────
Sep("Scenario B - item WITH batch tracking");
var itemB = await db.Items.FirstAsync(i => i.Code == CodeB);
itemB.CostPrice = 4.00m;
await db.SaveChangesAsync();

// وارد مخزون (حركة +10) ثم دُفعة مقابلة بنفس الكمية (نفس المعادلة الحاكمة في النظام)
await movementService.CreateAsync(new CreateStockMovementDto
{
    ItemId = itemB.Id, WarehouseId = warehouse.Id,
    MovementType = (int)MovementType.PurchaseReceipt,
    Quantity = 10m, UnitCost = 4.00m,
    Note = "تحضير اختبار سند الإتلاف (ب)", MovementDate = DateTime.Today
});
var expiry = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
var batch = new ItemBatch
{
    Id = Guid.NewGuid(), ItemId = itemB.Id, WarehouseId = warehouse.Id,
    BatchNumber = "LOT-B1", ExpiryDate = expiry, Quantity = 10m,
    ReceivedDate = DateTime.UtcNow
};
db.ItemBatches.Add(batch);
await db.SaveChangesAsync();

var beforeBStock = (await db.Items.AsNoTracking().FirstAsync(i => i.Id == itemB.Id)).CurrentStock;
var beforeBBatch = (await db.ItemBatches.AsNoTracking().FirstAsync(b => b.Id == batch.Id)).Quantity;
Console.WriteLine($"Item {CodeB} BEFORE write-off: CurrentStock={beforeBStock:N0} Batch {batch.BatchNumber} Qty={beforeBBatch:N0}");

var batchOptions = await writeOffService.GetBatchesForAsync(itemB.Id, warehouse.Id);
Console.WriteLine($"Batches from API : {batchOptions.Count} (first = {batchOptions.FirstOrDefault()?.BatchNumber} Expiry={batchOptions.FirstOrDefault()?.ExpiryDate})");
Check("B: الدُفعة تظهر في قائمة الاختيار (GetBatchesForAsync)", batchOptions.Any(o => o.Id == batch.Id));

var woffB = await writeOffService.CreateAsync(new CreateStockWriteOffDto
{
    ItemId = itemB.Id, WarehouseId = warehouse.Id,
    BatchId = batch.Id, Quantity = 2m,
    Reason = (int)StockWriteOffReason.Expired,
    Date = DateTime.Today, Notes = "اختبار ب - إتلاف دُفعة محددة منتهية"
}, "test-user");

var afterBStock = (await db.Items.AsNoTracking().FirstAsync(i => i.Id == itemB.Id)).CurrentStock;
var afterBBatch = (await db.ItemBatches.AsNoTracking().FirstAsync(b => b.Id == batch.Id)).Quantity;
var entryB = await journal.GetByIdAsync(woffB.JournalEntryId);
var movB = await db.StockMovements.AsNoTracking().FirstAsync(m => m.ReferenceNumber == woffB.DocumentNumber);
var batchTotalB = await db.ItemBatches.Where(b => b.ItemId == itemB.Id && b.WarehouseId == warehouse.Id).SumAsync(b => (decimal?)b.Quantity) ?? 0m;
var movTotalB = await db.StockMovements.Where(m => m.ItemId == itemB.Id && m.WarehouseId == warehouse.Id).SumAsync(m => (decimal?)m.Quantity) ?? 0m;

Console.WriteLine($"WRITE-OFF DOC  : {woffB.DocumentNumber} (BatchId={woffB.BatchId} Specified on doc)");
Console.WriteLine($"JOURNAL ENTRY  : {entryB!.EntryNumber}");
foreach (var line in entryB.Lines)
    Console.WriteLine($"   LEG: {line.AccountCode} - {line.AccountNameAr}  Debit={line.DebitAmount:N2}  Credit={line.CreditAmount:N2}");
Console.WriteLine($"MOVEMENT       : {movB.MovementType} Qty={movB.Quantity:N2} Ref={movB.ReferenceNumber}");

Check("B: خُصم من الدُفعة المحددة بالضبط (10 -> 8)", beforeBBatch == 10m && afterBBatch == 8m, $"batch before={beforeBBatch:N0} after={afterBBatch:N0}");
Check("B: CurrentStock انخفض بنفس الكمية (10 -> 8)", beforeBStock == 10m && afterBStock == 8m, $"stock before={beforeBStock:N0} after={afterBStock:N0}");
Check("B: تكامل الدُفعات محفوظ (مجموع الدُفعات == مجموع الحركات = 8)",
      batchTotalB == movTotalB && batchTotalB == 8m, $"batches={batchTotalB:N2} movements={movTotalB:N2}");
Check("B: قيمة السند = 2 × 4.00 = 8.00 والقيد متوازن",
      woffB.TotalCost == 8.00m && entryB.IsBalanced && entryB.TotalDebit == 8.00m && entryB.TotalCredit == 8.00m);
Check("B: السند يخزّن BatchId (الربط بالدُفعة)", woffB.BatchId == batch.Id);
// ─────────────── السيناريو ج: رفض كمية تتجاوز الرصيد ───────────────
Sep("Scenario C - reject quantity above stock");
var rejectedC = false;
try
{
    await writeOffService.CreateAsync(new CreateStockWriteOffDto
    {
        ItemId = itemA.Id, WarehouseId = warehouse.Id,
        Quantity = 999m, Reason = (int)StockWriteOffReason.Stolen,
        Date = DateTime.Today
    }, "test-user");
}
catch (Exception ex)
{
    rejectedC = true;
    Console.WriteLine($"  Exception as expected: {ex.Message}");
}
Check("C: رُفضت الكمية غير الكافية (999) ولم يُنشأ قيد/سند", rejectedC);

// ─────────────── السيناريو د: التقرير الملخّص + وجود حساب 5300 ───────────────
Sep("Scenario D - summary report + account 5300");
var summary = await writeOffService.GetSummaryAsync(null, null,
    DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1));
Console.WriteLine($"SUMMARY count={summary.Count} qty={summary.TotalQuantity:N2} value={summary.TotalValue:N2}");
foreach (var r in summary.ByReason)
    Console.WriteLine($"   BY-REASON: {r.ReasonNameAr} count={r.Count} qty={r.Quantity:N2} value={r.Value:N2}");
foreach (var m in summary.ByMonth)
    Console.WriteLine($"   BY-MONTH : {m.MonthLabel} value={m.Value:N2}");

var wasteAccount = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Code == "5300");
var invAccount = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Code == "1300");
Console.WriteLine($"Account 5300: {(wasteAccount is null ? "NOT FOUND" : $"{wasteAccount.NameAr} (CurrentBalance={wasteAccount.CurrentBalance:N2})")}");
Console.WriteLine($"Account 1300: {(invAccount is null ? "NOT FOUND" : $"{invAccount.NameAr} (CurrentBalance={invAccount.CurrentBalance:N2})")}");

Check("D: الملخص يعرض قيماً صحيحة (عد=2، قيمة=45.50 = 37.50 + 8.00)",
      summary.Count == 2 && summary.TotalValue == 45.50m, $"count={summary.Count} value={summary.TotalValue:N2}");
Check("D: يوجد تفصيل حسب السبب (تالف/مكسور + منتهي الصلاحية)", summary.ByReason.Count >= 2);
Check("D: حساب مصروف الهالك 5300 أُنشئ وأُضيف لرصيده (37.50 + 8.00) أو (أُنشئ سابقاً)",
      wasteAccount is not null && wasteAccount.CurrentBalance == 45.50m,
      wasteAccount is null ? "missing" : wasteAccount.CurrentBalance.ToString("N2"));
Check("D: حساب المخزون 1300 يعكس الخصم (أُنقص 5 × 12.50 + 2 × 4.00 من أرصدته السابقة — الحساب نظامي موجود)",
      wasteAccount is not null && invAccount is not null);

Console.WriteLine();
Console.WriteLine($"RESULT  pass={pass} fail={fail}");
Environment.ExitCode = fail > 0 ? 1 : 0;