namespace ERPSystem.Domain.Entities;

/// <summary>Marker written by daily backup script (Part G health check reads this).</summary>
public class BackupMarker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime LastSuccessAt { get; set; }
    public string? FilePath { get; set; }
}
