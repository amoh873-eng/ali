namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a logged unhandled exception (سجل الأخطاء — لوحة إدارة النظام).
/// Written by the exception-logging middleware whenever an error bubbles up the pipeline.
/// </summary>
public class ExceptionLog
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>UTC timestamp when the exception occurred.</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>The exception message (short summary).</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Full exception details including inner exceptions.</summary>
    public string? StackTrace { get; set; }

    /// <summary>Username of the authenticated user at the time (null when anonymous).</summary>
    public string? UserId { get; set; }

    /// <summary>The request path that triggered the exception.</summary>
    public string? RequestPath { get; set; }

    /// <summary>Fully-qualified exception type name (e.g. InvalidOperationException).</summary>
    public string? ExceptionType { get; set; }
}
