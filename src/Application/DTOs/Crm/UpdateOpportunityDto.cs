using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for updating an existing sales opportunity.
/// </summary>
public class UpdateOpportunityDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public Guid? CustomerId { get; set; }

    public Guid? LeadId { get; set; }

    [Range(1, 6)]
    public int Stage { get; set; }

    public decimal ExpectedValue { get; set; }

    public DateTime? ExpectedCloseDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
