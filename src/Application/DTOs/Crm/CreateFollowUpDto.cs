using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for creating a new scheduled follow-up.
/// </summary>
public class CreateFollowUpDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime FollowUpDate { get; set; } = DateTime.Today;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? LeadId { get; set; }

    public Guid? OpportunityId { get; set; }
}
