// Part F — JoFotara integration is OPTIONAL and feature-flagged ("JoFotara" module, off by default).
// This skeleton implements the contract against a configurable HTTP endpoint and stores QR/reference fields.
// Real ISTD/JoFotara API request/response shapes MUST be obtained from official documentation + sandbox
// credentials before wiring the actual field mapping. Until then this service logs and returns gracefully.
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Infrastructure.Services;

public class JoFotaraIntegrationService : IJoFotaraIntegrationService
{
    private readonly ERPSystem.Infrastructure.Data.AppDbContext _db;
    private readonly ISystemSettingsService _settings;
    private readonly IConfiguration _cfg;
    private readonly IHttpClientFactory? _httpFactory;
    private readonly ILogger<JoFotaraIntegrationService>? _logger;

    public JoFotaraIntegrationService(ERPSystem.Infrastructure.Data.AppDbContext db, ISystemSettingsService settings, IConfiguration cfg, IHttpClientFactory? httpFactory = null, ILogger<JoFotaraIntegrationService>? logger = null)
    { _db = db; _settings = settings; _cfg = cfg; _httpFactory = httpFactory; _logger = logger; }

    public bool IsEnabled()
    {
        try { var t = _settings.GetFeatureFlagsAsync(); t.Wait(1500); return t.IsCompletedSuccessfully && t.Result.TryGetValue("JoFotara", out var v) && v; } catch { return false; }
    }

    public async Task SubmitInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (!IsEnabled()) return;
        await DoSubmitAsync(invoiceId, ct);
    }

    public async Task RetryFailedAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var inv = await _db.Set<SalesInvoice>().FirstOrDefaultAsync(x => x.Id == invoiceId, ct);
        if (inv == null || inv.JoFotaraStatus != JoFotaraStatus.Failed) return;
        await DoSubmitAsync(invoiceId, ct);
    }

    private async Task DoSubmitAsync(Guid invoiceId, CancellationToken ct)
    {
        var inv = await _db.Set<SalesInvoice>().FirstOrDefaultAsync(x => x.Id == invoiceId, ct);
        if (inv == null) return;
        var endpoint = _cfg["JoFotara:Endpoint"];
        var apiKey = _cfg["JoFotara:ApiKey"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            inv.JoFotaraStatus = JoFotaraStatus.Failed;
            await _db.SaveChangesAsync(ct);
            _logger?.LogWarning("JoFotara not configured (Endpoint/ApiKey missing) — invoice {Id} marked Failed", invoiceId);
            return;
        }
        try
        {
            // NOTE: Request shape is TBD pending real ISTD docs. Sending minimal envelope for now — adapt once docs obtained.
            var payload = System.Text.Json.JsonSerializer.Serialize(new { invoiceId = inv.InvoiceNumber, total = inv.TotalAmount, date = inv.InvoiceDate });
            var client = _httpFactory?.CreateClient() ?? new HttpClient();
            var resp = await client.PostAsync(endpoint, new StringContent(payload, System.Text.Encoding.UTF8, "application/json"), ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                inv.JoFotaraStatus = JoFotaraStatus.Failed;
                await _db.SaveChangesAsync(ct);
                _logger?.LogWarning("JoFotara submit failed {Status} {Body}", resp.StatusCode, body);
                return;
            }
            // Try to extract qr/reference from response — best-effort until real schema known
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("qrCode", out var q)) inv.JoFotaraQrCode = q.GetString();
                if (doc.RootElement.TryGetProperty("reference", out var r)) inv.JoFotaraReferenceNumber = r.GetString();
                else if (doc.RootElement.TryGetProperty("referenceNumber", out var r2)) inv.JoFotaraReferenceNumber = r2.GetString();
                if (string.IsNullOrWhiteSpace(inv.JoFotaraReferenceNumber)) inv.JoFotaraReferenceNumber = body.Length > 200 ? body[..200] : body;
            }
            catch { inv.JoFotaraReferenceNumber = body.Length > 200 ? body[..200] : body; }
            inv.JoFotaraStatus = JoFotaraStatus.Submitted;
            inv.JoFotaraSubmittedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            inv.JoFotaraStatus = JoFotaraStatus.Failed;
            await _db.SaveChangesAsync(ct);
            _logger?.LogError(ex, "JoFotara submit exception for {Id}", invoiceId);
        }
    }
}
