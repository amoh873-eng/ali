namespace ERPSystem.Application.Interfaces;

public interface IBackupService
{
    Task<string> CreateLocalBackupAsync(CancellationToken ct = default);
    Task CopyToRemoteAsync(string localPath, CancellationToken ct = default);
}
