using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace ERPSystem.Infrastructure.Services;

/// <summary>Part H: deduped error notifier + daily digest. Email via SMTP if configured.</summary>
public class ErrorNotifierService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<ErrorNotifierService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _lastSent = new();
    private static readonly TimeSpan DedupWindow = TimeSpan.FromHours(1);
    public ErrorNotifierService(IConfiguration cfg, ILogger<ErrorNotifierService> logger){_cfg=cfg;_logger=logger;}
    public bool ShouldNotify(Exception ex)
    {
        var key = ex.GetType().Name + ":" + Normalize(ex.Message);
        if(_lastSent.TryGetValue(key, out var last) && DateTime.UtcNow - last < DedupWindow) return false;
        _lastSent[key]=DateTime.UtcNow; return true;
    }
    private static string Normalize(string m)=> m.Length>200? m[..200]: m;
    public async Task NotifyAsync(string subject, string body, CancellationToken ct=default)
    {
        var to = _cfg["Notifications:ToEmail"] ?? _cfg["SupportContactInfo"];
        var smtpHost = _cfg["Smtp:Host"];
        if(string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(to))
        { _logger.LogWarning("[NOTIFY] {Subject}: {Body}", subject, body); return; }
        try{
            using var mail=new System.Net.Mail.MailMessage();
            mail.From=new System.Net.Mail.MailAddress(_cfg["Smtp:From"] ?? to);
            mail.To.Add(to); mail.Subject=subject; mail.Body=body;
            using var smtp=new System.Net.Mail.SmtpClient(smtpHost, int.TryParse(_cfg["Smtp:Port"],out var p)?p:587);
            var user=_cfg["Smtp:User"]; var pass=_cfg["Smtp:Pass"];
            if(!string.IsNullOrWhiteSpace(user)) smtp.Credentials=new System.Net.NetworkCredential(user,pass);
            smtp.EnableSsl=true;
            await smtp.SendMailAsync(mail, ct);
        }catch(Exception ex){ _logger.LogError(ex,"Notify failed"); _logger.LogWarning("[NOTIFY FALLBACK] {Subject}: {Body}",subject,body); }
    }
    public async Task NotifyHealthChangeAsync(string name, string status, string? desc, CancellationToken ct=default)
    {
        await NotifyAsync($"[Health] {name} → {status}", $"{name}: {status}\n{desc}", ct);
    }
}
