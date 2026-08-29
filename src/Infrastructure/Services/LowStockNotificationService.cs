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
/// فحص خلفي يومي لنقص المخزون: يستدعي ItemService.GetLowStockAsync() (نفس منطق
/// الحد الأدنى المعتمد وليس منطقاً ثالثاً مكرراً) ويُنشئ إشعاراً واحداً لكل صنف
/// تحت الحد لكل مستخدم يملك دور Inventory — فقط عند الانتقال إلى نقص المخزون
/// (عبر Item.LastLowStockNotifiedAt) حتى لا يكرّر الإشعار كل دورة وهو ما زال
/// منخفضاً، ويعيد الإشعار بعد إعادة التزويد ثم الانخفاض مجدداً.
/// </summary>
public class LowStockNotificationService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<LowStockNotificationService> _logger;

    public LowStockNotificationService(IServiceProvider sp, ILogger<LowStockNotificationService> logger)
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
                _logger.LogError(ex, "Low-stock notification check failed");
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

        // 1) نعتمد على ItemService.GetLowStockAsync() لتحديد ما هو تحت الحد (مصدر واحد للحقيقة).
        var itemService = scope.ServiceProvider.GetRequiredService<IItemService>();
        var lowDtos = await itemService.GetLowStockAsync();
        var lowIds = lowDtos.Select(i => i.Id).ToHashSet();

        // 2) أرصدة المخزون لكل صنف + مستخدمو دور Inventory المستهدفون بالإشعار.
        var items = await db.Items.ToListAsync(ct);
        var inventoryUsers = await userManager.GetUsersInRoleAsync("Inventory");
        var today = DateTime.UtcNow.Date;
        var nowUtc = DateTime.UtcNow;

        foreach (var item in items)
        {
            var isLow = lowIds.Contains(item.Id);
            if (!isLow)
            {
                // أُعيد تزويده فوق الحد → صفّر العلامة حتى يُنشأ إشعار عند انخفاضه لاحقاً.
                if (item.LastLowStockNotifiedAt != null)
                    item.LastLowStockNotifiedAt = null;
                continue;
            }

            // هل دخلنا حالة النقص اليوم (أو لأول مرة)؟ فيُرسل إشعار — وإلا نتجاهل.
            var transitioned = item.LastLowStockNotifiedAt is null
                || item.LastLowStockNotifiedAt.Value.Date < today;
            if (transitioned)
            {
                if (inventoryUsers.Count > 0)
                {
                    var title = "تنبيه نقص مخزون";
                    var message = $"الصنف {item.Code} - {item.NameAr}: الرصيد {item.CurrentStock:N0} أقل من الحد الأدنى {item.MinStockLevel:N0}.";
                    foreach (var u in inventoryUsers)
                    {
                        await notifications.CreateAsync(u.Id, title, message,
                            "/inventory/items", NotificationCategory.LowStock,
                            $"low-stock:{item.Id}", ct);
                    }
                }
                item.LastLowStockNotifiedAt = nowUtc;
            }
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Low-stock check completed: {Low} low item(s), {Users} inventory user(s).",
            lowIds.Count, inventoryUsers.Count);
    }
}