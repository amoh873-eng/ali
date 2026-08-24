using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>Owner-defined custom report. Two modes: parametrized read-only SQL or variant of an existing IReportService report.</summary>
public class CustomReportDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ClientRequestNote { get; set; }
    public CustomReportType ReportType { get; set; } = CustomReportType.SqlQuery;

    /// <summary>When ReportType==SqlQuery: the SQL text (must be SELECT-only, validated). Null for ExistingReportVariant.</summary>
    public string? SqlQuery { get; set; }

    /// <summary>When ReportType==ExistingReportVariant: key like "TrialBalance", "IncomeStatement", "BalanceSheet", "AccountLedger".</summary>
    public string? ExistingReportKey { get; set; }

    /// <summary>JSON array of {name,type,label,required,defaultValue}. Example: [{"name":"from","type":"date","label":"From"}]</summary>
    public string? ParametersJson { get; set; }

    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedByOwnerAt { get; set; } = DateTime.UtcNow;
}
