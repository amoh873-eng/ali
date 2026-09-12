using System.Data;
using System.Data.Common;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace ERPSystem.Infrastructure.Services;
public class CustomReportService : ICustomReportService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly IReportService _report;
    public CustomReportService(AppDbContext db, IConfiguration cfg, IReportService report) { _db=db; _cfg=cfg; _report=report; }
    public Task<List<CustomReportDefinition>> GetAllAsync(CancellationToken ct=default) => _db.Set<CustomReportDefinition>().OrderBy(x=>x.Name).ToListAsync(ct);
    public Task<List<CustomReportDefinition>> GetEnabledAsync(CancellationToken ct=default) => _db.Set<CustomReportDefinition>().Where(x=>x.IsEnabled && !x.IsDeleted).OrderBy(x=>x.Name).ToListAsync(ct);
    public Task<CustomReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct=default) => _db.Set<CustomReportDefinition>().FirstOrDefaultAsync(x=>x.Id==id, ct);
    public async Task<CustomReportDefinition> CreateAsync(CustomReportDefinition e, CancellationToken ct=default)
    { if(string.IsNullOrWhiteSpace(e.Name)) throw new InvalidOperationException("Name required"); if(e.ReportType==CustomReportType.SqlQuery) ValidateSql(e.SqlQuery??""); e.Id=e.Id==Guid.Empty?Guid.NewGuid():e.Id; e.CreatedAt=DateTime.UtcNow; e.CreatedByOwnerAt=DateTime.UtcNow; _db.Set<CustomReportDefinition>().Add(e); await _db.SaveChangesAsync(ct); return e; }
    public async Task<CustomReportDefinition> UpdateAsync(Guid id, Action<CustomReportDefinition> m, CancellationToken ct=default)
    { var e=await _db.Set<CustomReportDefinition>().FirstOrDefaultAsync(x=>x.Id==id,ct)??throw new InvalidOperationException("Not found"); m(e); e.UpdatedAt=DateTime.UtcNow; if(e.ReportType==CustomReportType.SqlQuery) ValidateSql(e.SqlQuery??""); await _db.SaveChangesAsync(ct); return e; }
    public async Task DeleteAsync(Guid id, CancellationToken ct=default)
    { var e=await _db.Set<CustomReportDefinition>().FirstOrDefaultAsync(x=>x.Id==id,ct)??throw new InvalidOperationException("Not found"); e.IsDeleted=true; e.UpdatedAt=DateTime.UtcNow; await _db.SaveChangesAsync(ct); }
    public async Task<CustomReportResult> ExecuteAsync(Guid id, Dictionary<string,object?> p, CancellationToken ct=default)
    { var d=await GetByIdAsync(id,ct)??throw new InvalidOperationException("Not found"); if(!d.IsEnabled) throw new InvalidOperationException("Disabled"); if(d.ReportType==CustomReportType.ExistingReportVariant) return await ExecuteVariantAsync(d,p); return await ExecuteSqlAsync(d,p,ct); }
    private async Task<CustomReportResult> ExecuteVariantAsync(CustomReportDefinition def, Dictionary<string,object?> p, CancellationToken ct=default)
    { var k=def.ExistingReportKey??"TrialBalance"; DateTime from=p.TryGetValue("from",out var fv)&&DateTime.TryParse(fv?.ToString(),out var fd)?fd:DateTime.Today.AddMonths(-1); DateTime to=p.TryGetValue("to",out var tv)&&DateTime.TryParse(tv?.ToString(),out var td)?td:DateTime.Today; DateTime asOf=p.TryGetValue("asOf",out var av)&&DateTime.TryParse(av?.ToString(),out var ad)?ad:DateTime.Today; if(k=="TrialBalance"){var d=await _report.GetTrialBalanceAsync();var r=new CustomReportResult{Columns=new(){"Code","Account","Debit","Credit","Balance"}};foreach(var l in d.Lines) r.Rows.Add(new(){["Code"]=l.AccountCode,["Account"]=l.AccountNameAr,["Debit"]=l.TotalDebit,["Credit"]=l.TotalCredit,["Balance"]=l.Balance});return r;} if(k=="IncomeStatement"){var d=await _report.GetIncomeStatementAsync(from,to);var r=new CustomReportResult{Columns=new(){"Account","Amount","Type"}};foreach(var x in d.Revenues) r.Rows.Add(new(){["Account"]=x.AccountNameAr,["Amount"]=x.Amount,["Type"]="Revenue"});foreach(var x in d.Expenses) r.Rows.Add(new(){["Account"]=x.AccountNameAr,["Amount"]=x.Amount,["Type"]="Expense"});return r;} if(k=="BalanceSheet"){var d=await _report.GetBalanceSheetAsync(asOf);var r=new CustomReportResult{Columns=new(){"Account","Amount","Category"}};foreach(var x in d.Assets) r.Rows.Add(new(){["Account"]=x.AccountNameAr,["Amount"]=x.Amount,["Category"]="Asset"});foreach(var x in d.Liabilities) r.Rows.Add(new(){["Account"]=x.AccountNameAr,["Amount"]=x.Amount,["Category"]="Liability"});foreach(var x in d.Equity) r.Rows.Add(new(){["Account"]=x.AccountNameAr,["Amount"]=x.Amount,["Category"]="Equity"});return r;} throw new InvalidOperationException(k); }
    private async Task<CustomReportResult> ExecuteSqlAsync(CustomReportDefinition def, Dictionary<string,object?> parameters, CancellationToken ct)
    { var sql=def.SqlQuery??throw new InvalidOperationException("No SQL"); ValidateSql(sql); var allowed=GetAllowed(def.ParametersJson); var norm = parameters.ToDictionary(k=>k.Key, k=>k.Value, StringComparer.OrdinalIgnoreCase);
      // ADO مشغّل-مستقل عبر اتصال DbContext الفعلي (يدعم SQL Server وPostgreSQL معاً).
      var conn = _db.Database.GetDbConnection(); var wasClosed = conn.State!=ConnectionState.Open; if(wasClosed) await conn.OpenAsync(ct);
      try {
        await using var cmd = conn.CreateCommand(); cmd.CommandText=sql; cmd.CommandType=CommandType.Text;
        foreach(var name in allowed){ object? v=null; norm.TryGetValue(name,out v); if(v is string s && string.IsNullOrWhiteSpace(s)) v=null; var p=cmd.CreateParameter(); p.ParameterName="@"+name; p.Value=v??DBNull.Value; cmd.Parameters.Add(p); }
        using var reader=await cmd.ExecuteReaderAsync(ct); var result=new CustomReportResult(); for(int i=0;i<reader.FieldCount;i++) result.Columns.Add(reader.GetName(i)); while(await reader.ReadAsync(ct)){var row=new Dictionary<string,object?>(); foreach(var c in result.Columns) row[c]=reader[c] is DBNull?null:reader[c]; result.Rows.Add(row);} return result;
      } finally { if(wasClosed && conn.State==ConnectionState.Open) await conn.CloseAsync(); } }
    private static HashSet<string> GetAllowed(string? json)
    { if(string.IsNullOrWhiteSpace(json) || json.Trim()=="[]") return new(); try{ using var doc=System.Text.Json.JsonDocument.Parse(json); if(doc.RootElement.ValueKind!=System.Text.Json.JsonValueKind.Array) return new(); var hs=new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach(var el in doc.RootElement.EnumerateArray()) if(el.TryGetProperty("name", out var pv) && pv.ValueKind==System.Text.Json.JsonValueKind.String) { var s=pv.GetString(); if(!string.IsNullOrWhiteSpace(s)) hs.Add(s.Trim()); } return hs; }catch{return new();} }
    private static void ValidateSql(string sql)
    { var s=sql.Trim(); if(s.Contains(';') && s.TrimEnd(';').Contains(';')) throw new InvalidOperationException("Multiple statements not allowed"); var u=s.ToUpperInvariant(); if(u.StartsWith("INSERT")||u.StartsWith("UPDATE")||u.StartsWith("DELETE")||u.StartsWith("DROP")||u.StartsWith("ALTER")||u.StartsWith("CREATE")||u.StartsWith("TRUNCATE")||u.StartsWith("EXEC")) throw new InvalidOperationException("Only SELECT allowed"); if(!u.StartsWith("SELECT") && !u.StartsWith("WITH")) throw new InvalidOperationException("Must start with SELECT"); }
    public byte[] ExportExcel(CustomReportResult result, string sheetName="Report")
    {
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);
        for (int i = 0; i < result.Columns.Count; i++) ws.Cell(1, i + 1).Value = result.Columns[i];
        ws.Row(1).Style.Font.Bold = true;
        for (int r = 0; r < result.Rows.Count; r++)
            for (int c = 0; c < result.Columns.Count; c++)
                ws.Cell(r + 2, c + 1).Value = result.Rows[r].TryGetValue(result.Columns[c], out var v) ? (v?.ToString() ?? "") : "";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }
}
