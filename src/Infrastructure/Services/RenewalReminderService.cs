using ERPSystem.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Infrastructure.Services;

/// <summary>Once daily, checks NextRenewalDate and logs/email when renewal is approaching/overdue.</summary>
public class RenewalReminderService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<RenewalReminderService> _logger;
    public RenewalReminderService(IServiceProvider sp, ILogger<RenewalReminderService> logger) { _sp = sp; _logger = logger; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
                var s = await svc.GetAsync(stoppingToken);
                var days = (s.NextRenewalDate.Date - DateTime.UtcNow.Date).TotalDays;
                if (days < 0) _logger.LogWarning("Subscription OVERDUE by {Days} days (client: {Client})", -days, s.LicensedToClientName);
                else if (days <= 14) _logger.LogWarning("Renewal in {Days} days (client: {Client})", Math.Ceiling(days), s.LicensedToClientName);
                // TODO: send email to SupportContactInfo / configured owner inbox if desired — log is the single-tenant baseline.
            }
            catch (Exception ex) { _logger.LogError(ex, "Renewal check failed"); }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
