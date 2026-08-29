using ERPSystem.Application.DTOs.Notifications;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// تطبيق خدمة إشعارات الموظفين — يقرأ/يكتب جدول Notifications.
/// لا يعتمد على هوية المتصل هنا؛ المتصل (الفحص الخلفي أو الواجهة) يمرّر RecipientUserId صراحةً.
/// </summary>
public class StaffNotificationService : IStaffNotificationService
{
    private readonly DbContext _context;

    public StaffNotificationService(DbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(string userId, string title, string message, string? linkUrl,
        NotificationCategory category, string? dedupKey = null, CancellationToken ct = default)
    {
        _context.Set<Notification>().Add(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = userId,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            DedupKey = dedupKey,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            Category = category
        });
        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken ct = default)
    {
        var n = await _context.Set<Notification>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null) return;
        if (!n.IsRead)
        {
            n.IsRead = true;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        var items = await _context.Set<Notification>()
            .Where(x => x.RecipientUserId == userId && !x.IsRead)
            .ToListAsync(ct);
        if (items.Count == 0) return;
        foreach (var n in items) n.IsRead = true;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        => await _context.Set<Notification>()
            .CountAsync(x => x.RecipientUserId == userId && !x.IsRead, ct);

    public async Task<List<NotificationDto>> GetRecentAsync(string userId, int count, CancellationToken ct = default)
        => await _context.Set<Notification>()
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                Message = x.Message,
                LinkUrl = x.LinkUrl,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                Category = (int)x.Category
            })
            .ToListAsync(ct);

    public async Task<bool> ExistsDedupAsync(string userId, NotificationCategory category, string dedupKey,
        DateTime sinceUtc, CancellationToken ct = default)
        => await _context.Set<Notification>()
            .AnyAsync(x => x.RecipientUserId == userId
                        && x.Category == category
                        && x.DedupKey == dedupKey
                        && x.CreatedAt >= sinceUtc, ct);
}