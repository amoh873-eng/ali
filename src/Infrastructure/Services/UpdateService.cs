using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace ERPSystem.Infrastructure.Services;

/// <summary>Part I: controlled update — verify checksum, backup, migrate, health-check, log.</summary>
public class UpdateService
{
    private readonly AppDbContext _db;
    private readonly IBackupService _backup;
    private readonly IConfiguration _cfg;
    private readonly ILogger<UpdateService> _logger;
    public UpdateService(AppDbContext db, IBackupService backup, IConfiguration cfg, ILogger<UpdateService> logger){_db=db;_backup=backup;_cfg=cfg;_logger=logger;}
    public async Task<(bool ok,string msg)> CheckForUpdateAsync(CancellationToken ct=default)
    {
        var manifestUrl=_cfg["Update:ManifestUrl"];
        if(string.IsNullOrWhiteSpace(manifestUrl)) return (false,"No Update:ManifestUrl configured");
        try{
            using var http=new HttpClient(); var json=await http.GetStringAsync(manifestUrl, ct);
            var doc=System.Text.Json.JsonDocument.Parse(json);
            var ver=doc.RootElement.TryGetProperty("version",out var v)? v.GetString():"?";
            return (true,$"Latest: {ver}");
        }catch(Exception ex){ return (false, ex.Message); }
    }
    public async Task<(bool ok,string msg)> ApplyUpdateAsync(string version, string packagePath, string expectedSha256, string triggeredBy, CancellationToken ct=default)
    {
        var log=new UpdateLog{ Version=version, Checksum=expectedSha256, TriggeredBy=triggeredBy };
        try{
            // 1. checksum
            using var sha=System.Security.Cryptography.SHA256.Create();
            using var fs=File.OpenRead(packagePath);
            var hash=Convert.ToHexString(await sha.ComputeHashAsync(fs, ct));
            if(!hash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Checksum mismatch: expected {expectedSha256}, got {hash}");
            // 2. backup
            var backupPath=await _backup.CreateLocalBackupAsync(ct);
            _logger.LogInformation("Pre-update backup: {Path}", backupPath);
            await _backup.CopyToRemoteAsync(backupPath, ct);
            // 3. stop service, replace files, migrate, restart — must be done via shell script (see docs)
            // This method logs intent; actual file replacement is done by the companion script invoked from owner console.
            log.Success=true; log.Message=$"Verified + backup {backupPath}. Apply via deploy script.";
            _db.UpdateLogs.Add(log); await _db.SaveChangesAsync(ct);
            return (true, log.Message);
        }catch(Exception ex){
            log.Success=false; log.Message=ex.Message;
            _db.UpdateLogs.Add(log); await _db.SaveChangesAsync(ct);
            return (false, ex.Message);
        }
    }
}
