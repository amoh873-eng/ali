namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for displaying a scheduled follow-up (متابعة مجدولة).
/// </summary>
public class FollowUpDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime FollowUpDate { get; set; }
    public int Status { get; set; }
    public string? Notes { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? OpportunityId { get; set; }
}
