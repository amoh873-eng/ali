using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for creating a new sales opportunity.
/// </summary>
public class CreateOpportunityDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public Guid? CustomerId { get; set; }

    public Guid? LeadId { get; set; }

    /// <summary>
    /// 1..6 = Prospecting, Qualification, Proposal, Negotiation, Won, Lost.
    /// </summary>
    [Range(1, 6)]
    public int Stage { get; set; } = 1;

    public decimal ExpectedValue { get; set; }

    public DateTime? ExpectedCloseDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
