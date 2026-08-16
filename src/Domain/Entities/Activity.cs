using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a CRM activity (سجل التواصل) — a call, meeting, email, or note
/// linked to a customer, a lead, and/or an opportunity.
/// </summary>
public class Activity : BaseEntity
{
    /// <summary>
    /// Type of activity.
    /// </summary>
    public ActivityType Type { get; set; }

    /// <summary>
    /// Date/time of the activity.
    /// </summary>
    public DateTime ActivityDate { get; set; }

    /// <summary>
    /// Free-text description of what happened.
    /// </summary>
    public string Description { get; set; } = string.Empty;

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
