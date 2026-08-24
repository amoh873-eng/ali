namespace ERPSystem.Domain.Entities;

/// <summary>Daily health snapshot for trend (Part G — last 7 days).</summary>
public class HealthSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public string ResultsJson { get; set; } = "{}";
    public string OverallStatus { get; set; } = "Healthy";
}
