namespace ERPSystem.Domain.Entities;

/// <summary>Part I: audit log for each update attempt.</summary>
public class UpdateLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "";
    public string? Checksum { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? TriggeredBy { get; set; }
}
