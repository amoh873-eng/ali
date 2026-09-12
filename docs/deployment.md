# Deployment — Owner Console Extensions (Parts C–F)

## ⚠️ Database connection string — secrets policy (PostgreSQL migration)

The repository **must never contain a real database password**. Both `appsettings.json` and
`appsettings.Development.json` keep `ConnectionStrings:DefaultConnection` **empty** (`""`) on purpose.

### Local development
Use .NET **user-secrets** (stored per-user outside the repo, never committed):

```powershell
cd src/Web
dotnet user-secrets init                                 # if not present
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ERPSystemDb;Username=erp_app;Password=<YOUR_LOCAL_PASSWORD>"
```

The `UserSecretsId` lives in `ERPSystem.Web.csproj`; ASP.NET Core loads it automatically in the
`Development` environment — **no code change required**.

### Production / server deployment
Supply the connection string via an **environment variable** (the `__` separator maps to `:`):

```text
ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...
```

On Windows (service/scheduled task) set it in the service' environment or via:
```powershell
[System.Environment]::SetEnvironmentVariable('ConnectionStrings__DefaultConnection', $value, 'Machine')
```
On Linux systemd, put it under `[Service] Environment=`.

> 🔒 **Do not** put the real password back into any committed `appsettings*.json`. If a password
> is ever printed/shared in a chat or committed to Git, change the DB password immediately and treat
> the old one as leaked (a new password is cheap; a rebased shared repo is not).

## Read-only DB login for CustomReport SqlQuery
```sql
CREATE LOGIN erp_readonly WITH PASSWORD = 'Strong!Pass123';
CREATE USER erp_readonly FOR LOGIN erp_readonly;
ALTER ROLE db_datareader ADD MEMBER erp_readonly;
-- explicitly DENY write
DENY INSERT, UPDATE, DELETE ON SCHEMA::dbo TO erp_readonly;
```
`appsettings.json`: add `ConnectionStrings:ReadOnlyConnection` pointing to same DB with `User Id=erp_readonly`.

## Off-site backup (choose ONE approach)
- **Simple (recommended):** Extend `/etc/cron.daily/erp-backup` (or Windows Task) to run:
  `SqlCmd -S ... -Q "BACKUP DATABASE [ERPSystemDb] TO DISK='C:\backups\ERP_$(date +%Y%m%d).bak'"`
  then `scp C:\backups\*.bak backupuser@backup-host:/backups/` over SSH key restricted to that dir.
- **Owner-console-managed:** Set `SystemSettings.BackupDestinationType` = `RemoteSsh` and `BackupDestination` = `user@host:/path`, store SSH key in `Backup:RemoteHost/Path` secrets, call `IBackupService.CopyToRemoteAsync` from a `dotnet run --backup-only` cron wrapper.

Do NOT build both.

## Backup schedule hook
Windows Task / cron calls `BackupService.CreateLocalBackupAsync` then `CopyToRemoteAsync`.

## Part F — JoFotara
- Feature flag `JoFotara` (off by default). When enabled, `SalesInvoiceService` should call `IJoFotaraIntegrationService.SubmitInvoiceAsync` after posting (hook once, not scattered).
- Per-deployment credentials: `JoFotara:Endpoint`, `JoFotara:ApiKey` in secrets (never in DB row).
- JoFotara fields on SalesInvoice: `JoFotaraQrCode`, `JoFotaraReferenceNumber`, `JoFotaraSubmittedAt`, `JoFotaraStatus` (NotSubmitted/Submitted/Failed). Failure does NOT block sale — retry via button + optional background backoff.
- ⚠️ **BLOCKED until real ISTD docs + sandbox credentials obtained.** Do not guess API shape. Current `JoFotaraIntegrationService` is a skeleton behind the feature flag — it logs and marks Failed if unconfigured.

## Verification
- Part C: insert a CustomReportDefinition via `/{secret}/reports` (SqlQuery with `SELECT 1` + param), confirm it appears at `/reports/custom`, try `DELETE FROM ...` as SQL — must be rejected by `ValidateSql`.
- Part D: change `SubscriptionCycle` in `/{secret}/subscription`, confirm `NextRenewalDate` recomputes; check dashboard color (amber <14d, red <3d/overdue).
- Part E: run backup script, confirm file in local + remote; change a setting in owner console and check `SystemSettingsHistory` row.
- Part F: blocked pending real docs — report whether implemented against real sandbox or not.
