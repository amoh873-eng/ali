using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Infrastructure.Services;

/// <summary>
/// فحص خلفي يومي لتواريخ انتهاء صلاحية الدفعات (StockBatch) للصنف الذي يتتبّع
/// الانتهاء. يميّز بدرجات: منتهية فعلياً (أولوية أعلى) مقابل قرب الانتهاء ضمن
/// نافذة قابلة للضبط (ExpiryWarningWindowDays، افتراضي 7). يُنشئ إشعار من نوع
/// ExpiryWarning لمستخدمي دور Inventory عبر نظام إشعارات Part A. لا يكرّر لنفس
/// الدفعة يومياً (عبر StockBatch.LastExpiryNotifiedAt).
/// </summary>
public class ExpiryNotificationService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ExpiryNotificationService> _logger;

    public ExpiryNotificationService(IServiceProvider sp, ILogger<ExpiryNotificationService> logger)
    { _sp = sp; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunCheckAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expiry notification check failed");
            }
            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<IStaffNotificationService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var settings = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
        var s = await settings.GetAsync(ct);
        var windowDays = s.ExpiryWarningWindowDays <= 0 ? 7 : s.ExpiryWarningWindowDays;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var nowUtc = DateTime.UtcNow;

        var batches = await db.StockBatches
            .Where(b => b.Quantity > 0m && b.ExpiryDate != null)
            .Include(b => b.Item)
            .ToListAsync(ct);

        var inventoryUsers = await userManager.GetUsersInRoleAsync("Inventory");

        foreach (var batch in batches)
        {
            var expiry = batch.ExpiryDate!.Value;
            var expired = expiry < today;
            var expiringSoon = !expired && expiry <= today.AddDays(windowDays);
            if (!expired && !expiringSoon) continue;

            // أُرسل إشعار لليوم/لهذه الدفعة قبل؟ نتجاهل حتى لا نكرر كل دورة.
            var due = batch.LastExpiryNotifiedAt is null
                || DateOnly.FromDateTime(batch.LastExpiryNotifiedAt.Value) < today;
            if (!due) continue;

            if (inventoryUsers.Count > 0)
            {
                var itemName = batch.Item?.NameAr ?? batch.ItemId.ToString();
                string title;
                string message;
                if (expired)
                {
                    title = "تنبيه: دفعة منتهية الصلاحية";
                    message = $"الدفعة ({batch.BatchNumber ?? "-"}) للصنف {itemName} منتهية الصلاحية منذ {expiry:yyyy-MM-dd} وتبقى منها {batch.Quantity:N0}.";
                }
                else
                {
                    title = "تنبيه: دفعة تقترب من انتهاء الصلاحية";
                    message = $"الدفعة ({batch.BatchNumber ?? "-"}) للصنف {itemName} تنتهي يوم {expiry:yyyy-MM-dd} وتتبقى منها {batch.Quantity:N0}.";
                }

                foreach (var u in inventoryUsers)
                {
                    await notifications.CreateAsync(u.Id, title, message,
                        "/inventory/expiry", NotificationCategory.ExpiryWarning,
                        $"expiry:{batch.Id}", ct);
                }
            }
            batch.LastExpiryNotifiedAt = nowUtc;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Expiry check completed: {Batches} batch(es) within window/expired.", batches.Count);
    }
}