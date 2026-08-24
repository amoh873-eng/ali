using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace ERPSystem.Infrastructure.Health;
public class DiskSpaceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext c, CancellationToken ct=default)
    {
        try{ var root=Path.GetPathRoot(AppContext.BaseDirectory)??"C:\\"; var d=new DriveInfo(root); var pct=d.TotalSize==0?100:(double)d.AvailableFreeSpace/d.TotalSize*100;
        var data=new Dictionary<string,object>{["freePct"]=Math.Round(pct,1),["freeGb"]=Math.Round(d.AvailableFreeSpace/1024.0/1024/1024,1)};
        if(pct<10) return Task.FromResult(HealthCheckResult.Unhealthy($"Disk {pct:F1}% free",data:data));
        if(pct<20) return Task.FromResult(HealthCheckResult.Degraded($"Disk {pct:F1}% free",data:data));
        return Task.FromResult(HealthCheckResult.Healthy($"Disk {pct:F1}% free",data:data));}catch(Exception ex){return Task.FromResult(HealthCheckResult.Unhealthy(ex.Message));}
    }
}
public class SqlServiceHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db; public SqlServiceHealthCheck(AppDbContext db)=>_db=db;
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext c, CancellationToken ct=default)
    { try{ await _db.Database.ExecuteSqlRawAsync("SELECT 1",ct); return HealthCheckResult.Healthy("SQL reachable"); }catch(Exception ex){return HealthCheckResult.Unhealthy(ex.Message);} }
}
public class BackupHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db; private readonly IConfiguration _cfg; public BackupHealthCheck(AppDbContext db, IConfiguration cfg){_db=db;_cfg=cfg;}
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext c, CancellationToken ct=default)
    { try{ var m=await _db.BackupMarkers.OrderByDescending(x=>x.LastSuccessAt).FirstOrDefaultAsync(ct); DateTime? last=m?.LastSuccessAt;
        if(last==null){ var p=_cfg["Backup:MarkerFile"]??Path.Combine(AppContext.BaseDirectory,"backups","last_backup.txt"); if(File.Exists(p)){ var t=await File.ReadAllTextAsync(p,ct); if(DateTime.TryParse(t.Trim(),out var dt)) last=dt; else last=File.GetLastWriteTimeUtc(p);} }
        if(last==null) return HealthCheckResult.Degraded("No backup yet"); var age=DateTime.UtcNow-last.Value; var data=new Dictionary<string,object>{["lastSuccess"]=last.Value.ToString("u"),["ageHours"]=Math.Round(age.TotalHours,1)};
        if(age.TotalHours>26) return HealthCheckResult.Unhealthy($"Backup {age.TotalHours:F1}h ago",data:data);
        return HealthCheckResult.Healthy($"Backup {age.TotalHours:F1}h ago",data:data);}catch(Exception ex){return HealthCheckResult.Unhealthy(ex.Message);} }
}
public class ErrorRateHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db; public ErrorRateHealthCheck(AppDbContext db)=>_db=db;
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext c, CancellationToken ct=default)
    { try{ var since=DateTime.UtcNow.AddHours(-1); var n=await _db.ExceptionLogs.Where(x=>x.OccurredAt>=since).CountAsync(ct);
        var data=new Dictionary<string,object>{["errorsLastHour"]=n};
        if(n>20) return HealthCheckResult.Unhealthy($"{n} errors/hr",data:data);
        if(n>5) return HealthCheckResult.Degraded($"{n} errors/hr",data:data);
        return HealthCheckResult.Healthy($"{n} errors/hr",data:data);}catch(Exception ex){return HealthCheckResult.Unhealthy(ex.Message);} }
}
public class SslExpiryHealthCheck : IHealthCheck
{
    private readonly IConfiguration _cfg; public SslExpiryHealthCheck(IConfiguration cfg)=>_cfg=cfg;
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext c, CancellationToken ct=default)
    { try{ var p=_cfg["Ssl:CertPath"]; if(!string.IsNullOrWhiteSpace(p)&&File.Exists(p)){ var cert=new System.Security.Cryptography.X509Certificates.X509Certificate2(p); var d=(cert.NotAfter-DateTime.UtcNow).TotalDays;
        var data=new Dictionary<string,object>{["notAfter"]=cert.NotAfter.ToString("u"),["daysLeft"]=Math.Round(d,1)};
        if(d<0) return Task.FromResult(HealthCheckResult.Unhealthy($"Cert expired {Math.Abs(d):F0}d ago",data:data));
        if(d<14) return Task.FromResult(HealthCheckResult.Degraded($"Cert {d:F0}d left",data:data));
        return Task.FromResult(HealthCheckResult.Healthy($"Cert {d:F0}d left",data:data));}
        return Task.FromResult(HealthCheckResult.Healthy("No cert path (dev)"));}catch(Exception ex){return Task.FromResult(HealthCheckResult.Degraded(ex.Message));} }
}
