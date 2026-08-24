using ERPSystem.Domain.Entities;

namespace ERPSystem.Application.Interfaces;

public interface ISystemSettingsService
{
    Task<SystemSettings> GetAsync(CancellationToken ct = default);
    Task<SystemSettings> UpdateAsync(Action<SystemSettings> mutate, CancellationToken ct = default);
    bool IsModuleEnabled(string moduleKey);
    Task<Dictionary<string, bool>> GetFeatureFlagsAsync(CancellationToken ct = default);
}
