using ERPSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace ERPSystem.Infrastructure.Services;

/// <summary>Part K: when trial expires, flip IsDeploymentActive=false.</summary>
public class TrialExpiryService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<TrialExpiryService> _logger;
    public TrialExpiryService(IServiceProvider sp, ILogger<TrialExpiryService> logger){_sp=sp;_logger=logger;}
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            try{
                using var scope=_sp.CreateScope();
                var svc=scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
                var s=await svc.GetAsync(ct);
                if(s.IsTrialMode && s.TrialExpiresAt.HasValue && DateTime.UtcNow>=s.TrialExpiresAt.Value && s.IsDeploymentActive)
                {
                    await svc.UpdateAsync(x=> x.IsDeploymentActive=false, ct);
                    _logger.LogWarning("Trial expired for {Client} at {At}", s.LicensedToClientName, DateTime.UtcNow);
                }
            }catch(Exception ex){ _logger.LogError(ex,"Trial check failed");}
            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }
}
