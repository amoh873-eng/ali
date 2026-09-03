using ERPSystem.Domain.Entities;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

// Test-harness fixture for the scale-barcode interceptor.
//   setup   : temporarily stores the current (item "بطاطا" code, WeightBarcodeRuleJson) in
//             a state file, then sets an EAN-13 weight rule + a numeric 5-digit item code.
//   teardown: restores the saved values / removes an inserted settings row.
// This ONLY touches test data — it never changes the schema or app code.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var cmd = (args.Length > 0 ? args[0] : "").ToLowerInvariant();
var conn = "Server=(localdb)\\mssqllocaldb;Database=ERPSystemDb;Trusted_Connection=True;TrustServerCertificate=True";
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options;
var stateFile = @"d:\ERPSystem\_scale_test_state.txt";

var ruleJson = "{\"Prefix\":\"21\",\"ItemCodeStart\":2,\"ItemCodeLength\":5,\"ValueStart\":7,\"ValueLength\":5,\"ValueType\":0,\"DecimalPlaces\":3}";
var itemCode = "21150";
var potatoId = Guid.Parse("71dbae64-7076-4640-83a5-650b292f49b9"); // بطاطا

await using var db = new AppDbContext(options);
await db.Database.EnsureCreatedAsync();

var settings = await db.SystemSettings.FirstOrDefaultAsync();
var potato = await db.Items.FirstOrDefaultAsync(i => i.Id == potatoId);

if (cmd == "setup")
{
    var oldRule = settings?.WeightBarcodeRuleJson ?? "<NULL>";
    var oldCode = potato?.Code ?? "<MISSING>";
    await File.WriteAllLinesAsync(stateFile, new[] { oldRule, oldCode, settings is null ? "NOROW" : "ROW" });

    if (settings is null)
    {
        settings = new SystemSettings();
        db.SystemSettings.Add(settings);
    }
    settings.WeightBarcodeRuleJson = ruleJson;
    settings.UpdatedAt = DateTime.UtcNow;

    if (potato is not null)
    {
        potato.Code = itemCode;
        potato.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();
    Console.WriteLine($"SETUP_DONE settingsRow={settings is not null} itemFound={potato is not null}");
}
else if (cmd == "teardown")
{
    if (!File.Exists(stateFile))
    {
        Console.WriteLine("TEARDOWN no-state-file");
        return;
    }

    var saved = await File.ReadAllLinesAsync(stateFile);
    var oldRule = saved.Length > 0 ? saved[0] : "<NULL>";
    var oldCode = saved.Length > 1 ? saved[1] : "<MISSING>";
    var hadRow = saved.Length > 2 && saved[2] == "ROW";

    settings = await db.SystemSettings.FirstOrDefaultAsync();
    if (settings is not null)
    {
        if (hadRow)
        {
            settings.WeightBarcodeRuleJson = oldRule == "<NULL>" ? null : oldRule;
            settings.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.SystemSettings.Remove(settings);
        }
    }

    if (potato is not null)
    {
        if (oldCode == "<MISSING>") { db.Items.Remove(potato); }
        else { potato.Code = oldCode; potato.UpdatedAt = DateTime.UtcNow; }
    }

    await db.SaveChangesAsync();
    File.Delete(stateFile);
    Console.WriteLine("TEARDOWN_DONE");
}
else
{
    Console.WriteLine("USAGE: ScaleTestFixture <setup|teardown>");
}