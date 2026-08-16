using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for creating a new CRM activity.
/// </summary>
public class CreateActivityDto
{
    /// <summary>
    /// 1 = Call, 2 = Meeting, 3 = Email, 4 = Note.
    /// </summary>
    [Range(1, 4)]
    public int Type { get; set; } = 1;

    public DateTime ActivityDate { get; set; } = DateTime.Today;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public Guid? CustomerId { get; set; }

    public Guid? LeadId { get; set; }

    public Guid? OpportunityId { get; set; }
}
