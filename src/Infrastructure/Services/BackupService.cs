using ERPSystem.Application.Interfaces;
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
        var connStr = _cfg.GetConnectionString("DefaultConnection")!;

        // PostgreSQL: نسخة عبر pg_dump (تنسيق custom) — لا توجد أداة BACKUP DATABASE هناك.
        if (IsPostgresConnection(connStr))
            return await CreatePostgresBackupAsync(connStr, ct);

        // SQL Server: BACKUP DATABASE الكلاسيكي.
        var file = Path.Combine(_localDir, $"ERP_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak");
        var dbName = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr).InitialCatalog;
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
        await conn.OpenAsync(ct);
        using var cmd = new Microsoft.Data.SqlClient.SqlCommand($"BACKUP DATABASE [{dbName}] TO DISK = @p WITH INIT", conn);
        cmd.Parameters.AddWithValue("@p", file);
        await cmd.ExecuteNonQueryAsync(ct);
        return file;
    }

    private static bool IsPostgresConnection(string connStr)
        => connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase)
           || connStr.Contains("Server=localhost", StringComparison.OrdinalIgnoreCase) && connStr.Contains("Port=", StringComparison.OrdinalIgnoreCase);

    /// <summary>نسخة احتياطية عبر pg_dump (يُبحث عنه في PG_BIN ثم المسار الافتراضي للتثبيت المحلي).</summary>
    private async Task<string> CreatePostgresBackupAsync(string connStr, CancellationToken ct)
    {
        var file = Path.Combine(_localDir, $"ERP_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dump");
        var pgBin = _cfg["Backup:PgBin"]
                    ?? Environment.GetEnvironmentVariable("PG_BIN")
                    ?? @"D:\PostgreSQL\pgsql\bin";
        var pgDump = Path.Combine(pgBin, "pg_dump.exe");
        if (!File.Exists(pgDump))
            throw new InvalidOperationException($"pg_dump غير موجود في '{pgDump}'. اضبط Backup:PgBin أو متغير PG_BIN.");

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connStr);
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pgDump,
            ArgumentList =
            {
                "-h", builder.Host,
                "-p", builder.Port.ToString(),
                "-U", builder.Username,
                "-Fc",
                "-f", file,
                builder.Database
            },
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.Environment["PGPASSWORD"] = builder.Password;

        using var p = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("تعذر تشغيل pg_dump.");
        var err = await p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"pg_dump فشل ({p.ExitCode}): {err}");
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
