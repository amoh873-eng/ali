namespace ERPSystem.Application.Interfaces;

public interface IJoFotaraIntegrationService
{
    Task SubmitInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task RetryFailedAsync(Guid invoiceId, CancellationToken ct = default);
    bool IsEnabled();
}
