using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace ERPSystem.Infrastructure.Services;

/// <summary>Daily digest + immediate health-change notifier (Part H).</summary>
public class NotificationDigestService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<NotificationDigestService> _logger;
    private readonly Dictionary<string,string> _lastStatus=new();
    public NotificationDigestService(IServiceProvider sp, ILogger<NotificationDigestService> logger){_sp=sp;_logger=logger;}
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // daily digest at ~9am UTC (approx — simple 24h loop)
        while(!ct.IsCancellationRequested)
        {
            try{ await SendDigestAsync(ct); }catch(Exception ex){ _logger.LogError(ex,"Digest failed");}
            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
    }
    private async Task SendDigestAsync(CancellationToken ct)
    {
        using var scope=_sp.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifier=scope.ServiceProvider.GetRequiredService<ErrorNotifierService>();
        var errors=await db.ExceptionLogs.Where(x=>x.OccurredAt>=DateTime.UtcNow.AddDays(-1)).CountAsync(ct);
        var subject=errors==0? "[Daily] All healthy": $"[Daily] {errors} errors/24h";
        var body=$"Errors(24h): {errors}\nTime: {DateTime.UtcNow:u}\nDaily digest — if this stops arriving, the notification pipeline or server may be down.";
        await notifier.NotifyAsync(subject, body, ct);
        _logger.LogInformation("Digest sent: {Subject}",subject);
    }
    public async Task OnHealthChangedAsync(string name, string status, string? desc)
    {
        _lastStatus[name]=status;
        try{
            using var scope=_sp.CreateScope();
            var notifier=scope.ServiceProvider.GetRequiredService<ErrorNotifierService>();
            await notifier.NotifyHealthChangeAsync(name,status,desc);
        }catch(Exception ex){ _logger.LogError(ex,"Health notify failed");}
    }
}
