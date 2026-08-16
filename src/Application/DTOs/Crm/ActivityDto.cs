namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for displaying a CRM activity (سجل التواصل).
/// </summary>
public class ActivityDto
{
    public Guid Id { get; set; }
    public int Type { get; set; }
    public DateTime ActivityDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? LeadId { get; set; }
    public string? LeadName { get; set; }
    public Guid? OpportunityId { get; set; }
    public string? OpportunityTitle { get; set; }
}
