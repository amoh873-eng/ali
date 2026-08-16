namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for displaying a sales opportunity (فرصة بيعية).
/// </summary>
public class OpportunityDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int Stage { get; set; }
    public decimal ExpectedValue { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string? Notes { get; set; }
}
