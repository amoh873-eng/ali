using System.Text;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

// ============================================================================
// DataCleanup — أداة فحص/تنفيذ تنظيف البيانات.
//   query : فحص فقط (قراءة) — الأصناف ذات المخزون < 1 ومراجعها، الجردات، الفواتير، بنود التسوية.
//   apply : ينفذ الحذف فعلياً.
// ============================================================================
Console.OutputEncoding = Encoding.UTF8;

var mode = args.FirstOrDefault() ?? "query";
if (mode == "query") await RunQueryOnlyAsync();
else if (mode == "apply") await RunApplyAsync();
else Console.WriteLine("Usage: DataCleanup <query|apply>");

static async Task RunQueryOnlyAsync()
{
    var db = new AppDbContextFactory().CreateDbContext(Array.Empty<string>());
    db.Database.SetCommandTimeout(300);

    var outPath = Path.Combine(Path.GetTempPath(), "DataCleanup_report.txt");
    using var sw = new StreamWriter(outPath, false, new UTF8Encoding(true));
    sw.WriteLine("DataCleanup report - items with CurrentStock < 1");
    sw.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    sw.WriteLine("Format: Code | Name | stock | refs | mv ib sb si sr q pi po pr sc wo");
    sw.WriteLine(new string('-', 120));

    var lowItems = await db.Items.AsNoTracking()
        .Where(i => i.CurrentStock < 1m)
        .OrderBy(i => i.Code)
        .ToListAsync();

    Console.WriteLine();
    Console.WriteLine($"--- Items with CurrentStock < 1 : {lowItems.Count} ---");

    var cleanItems = new List<Guid>();
    var refItems = new List<Guid>();

    foreach (var it in lowItems)
    {
        var movements = await db.StockMovements.AsNoTracking().CountAsync(m => m.ItemId == it.Id);
        var itemBatches = await db.ItemBatches.AsNoTracking().CountAsync(b => b.ItemId == it.Id);
        var stockBatches = await db.StockBatches.AsNoTracking().CountAsync(b => b.ItemId == it.Id);
        var saleLines = await db.SalesInvoiceLines.AsNoTracking().CountAsync(l => l.ItemId == it.Id);
        var saleReturnLines = await db.SalesReturnLines.AsNoTracking().CountAsync(l => l.ItemId == it.Id);
        var quoteLines = await db.SalesQuoteLines.AsNoTracking().CountAsync(l => l.ItemId == it.Id);
        var purchaseLines = await db.PurchaseInvoiceLines.AsNoTracking().CountAsync(l => l.ItemId == it.Id);
        var orderLines = await db.PurchaseOrderLines.AsNoTracking().CountAsync(l => l.ItemId == it.Id);
        var purchaseReturnLines = await db.PurchaseReturnLines.AsNoTracking().CountAsync(l => l.ItemId == it.Id);
        var stockCountLines = await db.StockCountLines.AsNoTracking().CountAsync(l => l.ItemId == it.Id);
        var writeOffs = await db.StockWriteOffs.AsNoTracking().CountAsync(w => w.ItemId == it.Id);

        var refs = movements + itemBatches + stockBatches + saleLines + saleReturnLines + quoteLines
                   + purchaseLines + orderLines + purchaseReturnLines + stockCountLines + writeOffs;

        var line = $"{it.Code} | {it.NameAr} | stock={it.CurrentStock:N2} | refs={refs} | " +
                   $"mv={movements} ib={itemBatches} sb={stockBatches} si={saleLines} sr={saleReturnLines} q={quoteLines} " +
                   $"pi={purchaseLines} po={orderLines} pr={purchaseReturnLines} sc={stockCountLines} wo={writeOffs}";

        Console.WriteLine(line);
        sw.WriteLine(line);

        if (refs == 0) cleanItems.Add(it.Id);
        else refItems.Add(it.Id);
    }

    Console.WriteLine();
    Console.WriteLine($"  Items with NO references: {cleanItems.Count}");
    Console.WriteLine($"  Items WITH references: {refItems.Count}");

    var lowIds = lowItems.Select(i => i.Id).ToHashSet();
    sw.WriteLine(new string('-', 120));
    sw.WriteLine($"SUMMARY: lowStock={lowItems.Count} clean={cleanItems.Count} withRefs={refItems.Count}");
    await sw.FlushAsync();

    // ── 2) الأصناف التي مرجعها الوحيد سطر جرد ──
    int scOnly = 0, scAndSales = 0;
    foreach (var it in lowItems)
    {
        var hasMov = await db.StockMovements.AsNoTracking().AnyAsync(m => m.ItemId == it.Id);
        var hasBatch = await db.ItemBatches.AsNoTracking().AnyAsync(b => b.ItemId == it.Id)
                       || await db.StockBatches.AsNoTracking().AnyAsync(b => b.ItemId == it.Id);
        var hasSales = await db.SalesInvoiceLines.AsNoTracking().AnyAsync(l => l.ItemId == it.Id)
                       || await db.SalesReturnLines.AsNoTracking().AnyAsync(l => l.ItemId == it.Id)
                       || await db.SalesQuoteLines.AsNoTracking().AnyAsync(l => l.ItemId == it.Id);
        var hasPurchase = await db.PurchaseInvoiceLines.AsNoTracking().AnyAsync(l => l.ItemId == it.Id)
                          || await db.PurchaseOrderLines.AsNoTracking().AnyAsync(l => l.ItemId == it.Id)
                          || await db.PurchaseReturnLines.AsNoTracking().AnyAsync(l => l.ItemId == it.Id);
        var hasSC = await db.StockCountLines.AsNoTracking().AnyAsync(l => l.ItemId == it.Id);

        if (!hasMov && !hasBatch && !hasSales && !hasPurchase && hasSC) scOnly++;
        if (hasSales) scAndSales++;
    }
    Console.WriteLine($"  Items referenced ONLY by a stock-count line: {scOnly}");
    Console.WriteLine($"  Items linked to sales documents: {scAndSales}");
    var purchaseLinked = await db.PurchaseInvoiceLines.AsNoTracking()
        .Where(l => lowIds.Contains(l.ItemId)).CountAsync();
    Console.WriteLine($"  Purchase-document lines referencing low-stock items: {purchaseLinked}");
// ── 3) الجردات المتأثرة ──
    var affectedCounts = await db.StockCountLines.AsNoTracking()
        .Where(l => lowIds.Contains(l.ItemId))
        .GroupBy(l => l.StockCountId)
        .Select(g => new { Id = g.Key, Lines = g.Count() })
        .ToListAsync();
    Console.WriteLine($"\nAffected stock counts: {affectedCounts.Count}");
    foreach (var a in affectedCounts)
    {
        var sc = await db.StockCounts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == a.Id);
        if (sc is null) continue;
        Console.WriteLine($"  {sc.StockCountNumber} | {sc.CountDate:yyyy-MM-dd} | lines={a.Lines} | note={sc.Note}");
    }

    // ── 4) الفواتير المرتبطة بأصناف منخفضة المخزون ──
    var affectedInvoices = await db.SalesInvoiceLines.AsNoTracking()
        .Where(l => lowIds.Contains(l.ItemId))
        .Select(l => l.SalesInvoiceId).Distinct()
        .ToListAsync();
    Console.WriteLine($"\nSales invoices referencing low-stock items: {affectedInvoices.Count}");
    if (affectedInvoices.Count <= 20)
    {
        foreach (var iid in affectedInvoices)
        {
            var inv = await db.SalesInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == iid);
            if (inv is null) continue;
            Console.WriteLine($"  {inv.InvoiceNumber} | {inv.InvoiceDate:yyyy-MM-dd} | {inv.TotalAmount:N2}");
        }
    }

    // ── 5) سجلات التسوية ──
    Console.WriteLine("\n--- BankCardStatements ---");
    var bankCount = await db.BankCardStatements.AsNoTracking().CountAsync();
    Console.WriteLine($"TOTAL rows: {bankCount}");
    var bankSample = await db.BankCardStatements.AsNoTracking()
        .OrderByDescending(b => b.ImportedAt).Take(10).ToListAsync();
    foreach (var b in bankSample)
        Console.WriteLine($"  {b.Reference} | {b.Amount:N2} | {b.TransactionDate:yyyy-MM-dd} | imported={b.ImportedAt:yyyy-MM-dd} by={b.ImportedBy}");

    Console.WriteLine();
    Console.WriteLine($"Report saved: {outPath}");
    Console.WriteLine("=== QUERY-ONLY finished - nothing deleted. Use: DataCleanup apply ===");
}
static async Task RunApplyAsync()
{
    var db = new AppDbContextFactory().CreateDbContext(Array.Empty<string>());
    db.Database.SetCommandTimeout(600);

    Console.WriteLine("=== EXECUTING DELETIONS (apply) ===");

    var lowItems = await db.Items.Where(i => i.CurrentStock < 1m).ToListAsync();
    var lowIds = lowItems.Select(i => i.Id).ToHashSet();
    Console.WriteLine($"Low-stock items targeted: {lowItems.Count}");

    // الأصناف المرتبطة بمستندات (فواتير/أوامر/مردودات) — لا تُحذف حفاظاً على المحاسبة
    var docLinkedIds = new HashSet<Guid>();
    foreach (var id in lowIds)
    {
        var hasDoc = await db.SalesInvoiceLines.AnyAsync(l => l.ItemId == id)
                     || await db.SalesReturnLines.AnyAsync(l => l.ItemId == id)
                     || await db.SalesQuoteLines.AnyAsync(l => l.ItemId == id)
                     || await db.PurchaseInvoiceLines.AnyAsync(l => l.ItemId == id)
                     || await db.PurchaseOrderLines.AnyAsync(l => l.ItemId == id)
                     || await db.PurchaseReturnLines.AnyAsync(l => l.ItemId == id);
        if (hasDoc) docLinkedIds.Add(id);
    }
    Console.WriteLine($"  Items linked to documents (kept): {docLinkedIds.Count}");

    var deletable = lowIds.Except(docLinkedIds).ToList();
    Console.WriteLine($"  Items to hard-delete: {deletable.Count}");

    foreach (var id in deletable)
    {
        db.StockMovements.RemoveRange(db.StockMovements.Where(m => m.ItemId == id));
        db.ItemBatches.RemoveRange(db.ItemBatches.Where(b => b.ItemId == id));
        db.StockBatches.RemoveRange(db.StockBatches.Where(b => b.ItemId == id));
        db.StockCountLines.RemoveRange(db.StockCountLines.Where(l => l.ItemId == id));
        db.StockWriteOffs.RemoveRange(db.StockWriteOffs.Where(w => w.ItemId == id));
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item is not null) db.Items.Remove(item);
    }
    await db.SaveChangesAsync();
    Console.WriteLine($"  Deleted {deletable.Count} items (with their movements/batches/count-lines/write-offs).");

    // حذف كل سجلات تسوية بطاقات الدفع (كشف البنك المستورد)
    var bankRows = await db.BankCardStatements.Where(b => !b.IsDeleted).ToListAsync();
    foreach (var b in bankRows) b.IsDeleted = true;
    await db.SaveChangesAsync();
    Console.WriteLine($"  Soft-deleted {bankRows.Count} BankCardStatement rows.");

    Console.WriteLine("=== DONE ===");
}