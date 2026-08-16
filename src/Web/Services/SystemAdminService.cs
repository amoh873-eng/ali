using ERPSystem.Domain.Entities;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Web.Services;

/// <summary>
/// DTO لعرض سجل خطأ في لوحة إدارة النظام.
/// </summary>
public class ExceptionLogDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? UserId { get; set; }
    public string? RequestPath { get; set; }
    public string? ExceptionType { get; set; }
}

/// <summary>
/// نتيجة فحص الاتصال بقاعدة البيانات.
/// </summary>
public class DatabaseHealthDto
{
    public bool Ok { get; set; }
    public long ElapsedMs { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// خدمات لوحة إدارة النظام: سجل الأخطاء وفحوص الصحة (قراءة فقط).
/// </summary>
public interface ISystemAdminService
{
    Task<List<ExceptionLogDto>> GetErrorsAsync(DateTime? from, DateTime? to, string? exceptionType, int max = 500);
    Task<int> GetErrorCountSinceAsync(DateTime since);
    Task<DatabaseHealthDto> CheckDatabaseAsync();
    Task<List<string>> GetPendingMigrationsAsync();
}

public class SystemAdminService : ISystemAdminService
{
    private readonly AppDbContext _db;

    public SystemAdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ExceptionLogDto>> GetErrorsAsync(DateTime? from, DateTime? to, string? exceptionType, int max = 500)
    {
        var query = _db.ExceptionLogs.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(e => e.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredAt <= to.Value.AddDays(1)); // يشمل اليوم كاملاً

        if (!string.IsNullOrWhiteSpace(exceptionType))
            query = query.Where(e => e.ExceptionType != null && e.ExceptionType.Contains(exceptionType));

        var logs = await query
            .OrderByDescending(e => e.OccurredAt)
            .Take(max)
            .ToListAsync();

        return logs.Select(e => new ExceptionLogDto
        {
            Id = e.Id,
            OccurredAt = e.OccurredAt,
            Message = e.Message,
            StackTrace = e.StackTrace,
            UserId = e.UserId,
            RequestPath = e.RequestPath,
            ExceptionType = e.ExceptionType
        }).ToList();
    }

    public async Task<int> GetErrorCountSinceAsync(DateTime since)
    {
        return await _db.ExceptionLogs.CountAsync(e => e.OccurredAt >= since);
    }

    public async Task<DatabaseHealthDto> CheckDatabaseAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1");
            sw.Stop();
            return new DatabaseHealthDto { Ok = true, ElapsedMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DatabaseHealthDto { Ok = false, ElapsedMs = sw.ElapsedMilliseconds, Error = ex.Message };
        }
    }

    public async Task<List<string>> GetPendingMigrationsAsync()
    {
        return (await _db.Database.GetPendingMigrationsAsync()).ToList();
    }
}
