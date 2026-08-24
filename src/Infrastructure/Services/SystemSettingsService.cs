using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ERPSystem.Infrastructure.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "SystemSettings_Singleton";

    public SystemSettingsService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<SystemSettings> GetAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<SystemSettings>(CacheKey, out var cached) && cached != null)
            return cached;

        var entity = await _db.SystemSettings.FindAsync(new object[] { SystemSettings.SingletonId }, ct);
        if (entity == null)
        {
            entity = new SystemSettings
            {
                Id = SystemSettings.SingletonId,
                LicensedToClientName = "Default Client",
                IsDeploymentActive = true,
                FeatureFlagsJson = SystemSettings.DefaultFeatureFlagsJson(),
                CreatedAt = DateTime.UtcNow
            };
            _db.SystemSettings.Add(entity);
            await _db.SaveChangesAsync(ct);
        }

        _cache.Set(CacheKey, entity, TimeSpan.FromMinutes(10));
        return entity;
    }

    public async Task<SystemSettings> UpdateAsync(Action<SystemSettings> mutate, CancellationToken ct = default)
    {
        var entity = await _db.SystemSettings.FindAsync(new object[] { SystemSettings.SingletonId }, ct);
        if (entity == null)
        {
            entity = new SystemSettings
            {
                Id = SystemSettings.SingletonId,
                LicensedToClientName = "Default Client",
                IsDeploymentActive = true,
                FeatureFlagsJson = SystemSettings.DefaultFeatureFlagsJson(),
                CreatedAt = DateTime.UtcNow
            };
            _db.SystemSettings.Add(entity);
        }

        // history snapshot (before state)
        var beforeJson = System.Text.Json.JsonSerializer.Serialize(entity);
        mutate(entity);
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        try
        {
            var hist = new SystemSettingsHistory { Id = Guid.NewGuid(), SystemSettingsId = entity.Id, ChangedAt = DateTime.UtcNow, SnapshotJson = beforeJson };
            _db.SystemSettingsHistory.Add(hist);
            await _db.SaveChangesAsync(ct);
        } catch { }
        _cache.Remove(CacheKey);
        _cache.Set(CacheKey, entity, TimeSpan.FromMinutes(10));
        return entity;
    }

    public bool IsModuleEnabled(string moduleKey)
    {
        // Synchronous helper for middleware — reads from cache or DB synchronously if needed
        // Prefer async path; this is for non-async contexts
        var task = GetFeatureFlagsAsync();
        task.Wait(2000);
        if (task.IsCompletedSuccessfully)
            return task.Result.TryGetValue(moduleKey, out var v) ? v : true;
        return true;
    }

    public async Task<Dictionary<string, bool>> GetFeatureFlagsAsync(CancellationToken ct = default)
    {
        var s = await GetAsync(ct);
        return s.GetFeatureFlags();
    }

    public void InvalidateCache() => _cache.Remove(CacheKey);
}
