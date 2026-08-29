using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

// DbProbe: runs the SQL in the file given by arg[0] and prints each result row as pipe-separated values.
// Connection string from env DBPROBE_CONN; defaults to localdb ERPSystemDb_FuncTest.
Console.OutputEncoding = Encoding.UTF8;
var conn = Environment.GetEnvironmentVariable("DBPROBE_CONN")
    ?? "Server=(localdb)\\mssqllocaldb;Database=ERPSystemDb_FuncTest;Trusted_Connection=True;TrustServerCertificate=True";

var sqlFile = args.Length > 0 ? args[0] : "q.sql";
if (!File.Exists(sqlFile)) { Console.WriteLine("ERR: file not found " + sqlFile); return; }
var sql = File.ReadAllText(sqlFile);

try
{
    using var cn = new SqlConnection(conn);
    await cn.OpenAsync();
    using var cmd = new SqlCommand(sql, cn) { CommandTimeout = 30 };
    using var r = await cmd.ExecuteReaderAsync();
    int rowCount = 0;
    while (await r.ReadAsync())
    {
        var parts = new List<string>();
        for (int i = 0; i < r.FieldCount; i++)
            parts.Add(r.IsDBNull(i) ? "NULL" : r.GetValue(i).ToString());
        Console.WriteLine(string.Join("|", parts));
        rowCount++;
    }
    Console.WriteLine($"[ROWS={rowCount}]");
}
catch (Exception ex)
{
    Console.WriteLine("ERR: " + ex.Message);
    Environment.ExitCode = 1;
}