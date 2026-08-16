using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a scheduled follow-up reminder (متابعة مجدولة) linked to a customer,
/// a lead, and/or an opportunity.
/// </summary>
public class FollowUp : BaseEntity
{
    /// <summary>
    /// Short title describing the follow-up.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// When the follow-up is due.
    /// </summary>
    public DateTime FollowUpDate { get; set; }

    /// <summary>
    /// Status: pending or done.
    /// </summary>
    public FollowUpStatus Status { get; set; }

    /// <summary>
    /// Optional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Optional link to a customer.
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the customer.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Optional link to a lead.
    /// </summary>
    public Guid? LeadId { get; set; }

    /// <summary>
    /// Navigation property to the lead.
    /// </summary>
    public Lead? Lead { get; set; }

    /// <summary>
    /// Optional link to an opportunity.
    /// </summary>
    public Guid? OpportunityId { get; set; }

    /// <summary>
    /// Navigation property to the opportunity.
    /// </summary>
    public Opportunity? Opportunity { get; set; }
}
