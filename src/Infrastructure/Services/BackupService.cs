using ERPSystem.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ERPSystem.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly IConfiguration _cfg;
    private readonly string _localDir;

    public BackupService(IConfiguration cfg)
    {
        _cfg = cfg;
        _localDir = cfg["Backup:LocalDir"] ?? Path.Combine(AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(_localDir);
    }

    public async Task<string> CreateLocalBackupAsync(CancellationToken ct = default)
    {
        var file = Path.Combine(_localDir, $"ERP_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak");
        var connStr = _cfg.GetConnectionString("DefaultConnection")!;
        var dbName = new SqlConnectionStringBuilder(connStr).InitialCatalog;
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand($"BACKUP DATABASE [{dbName}] TO DISK = @p WITH INIT", conn);
        cmd.Parameters.AddWithValue("@p", file);
        await cmd.ExecuteNonQueryAsync(ct);
        return file;
    }

    public async Task CopyToRemoteAsync(string localPath, CancellationToken ct = default)
    {
        var type = _cfg["Backup:DestinationType"] ?? "None";
        if (type == "None") return;
        // Documented approach: destination credentials in config/secrets, not in SystemSettings row.
        // For RemoteSsh: use scp/rsync via Process — simplest and auditable.
        if (type == "RemoteSsh")
        {
            var host = _cfg["Backup:RemoteHost"]; var dest = _cfg["Backup:RemotePath"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(dest)) return;
            var psi = new System.Diagnostics.ProcessStartInfo("scp", $"{localPath} {host}:{dest}") { UseShellExecute = false };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p != null) await p.WaitForExitAsync(ct);
        }
        else if (type == "S3Compatible")
        {
            // Minimal stub — real implementation would use AWS SDK. Keep single approach: scp is primary for now.
            throw new NotSupportedException("S3 upload requires AWS SDK — use RemoteSsh or configure external rclone.");
        }
    }
}
