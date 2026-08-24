using ERPSystem.Domain.Entities;

namespace ERPSystem.Application.Interfaces;

public class CustomReportResult
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}

public interface ICustomReportService
{
    Task<List<CustomReportDefinition>> GetAllAsync(CancellationToken ct = default);
    Task<List<CustomReportDefinition>> GetEnabledAsync(CancellationToken ct = default);
    Task<CustomReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomReportDefinition> CreateAsync(CustomReportDefinition entity, CancellationToken ct = default);
    Task<CustomReportDefinition> UpdateAsync(Guid id, Action<CustomReportDefinition> mutate, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<CustomReportResult> ExecuteAsync(Guid id, Dictionary<string, object?> parameters, CancellationToken ct = default);
    byte[] ExportExcel(CustomReportResult result, string sheetName = "Report");
}
