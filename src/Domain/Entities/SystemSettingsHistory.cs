namespace ERPSystem.Domain.Entities;

/// <summary>Append-only snapshot of SystemSettings taken BEFORE each owner update, for audit + rollback.</summary>
public class SystemSettingsHistory
{
    public Guid Id { get; set; }
    public Guid SystemSettingsId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? ChangedBy { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string? ChangeNote { get; set; }
}
