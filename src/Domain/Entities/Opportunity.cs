using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a sales opportunity (فرصة بيعية) linked to a Customer and/or a Lead.
/// </summary>
public class Opportunity : BaseEntity
{
    /// <summary>
    /// Opportunity title (e.g., "توريد 100 جهاز").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional link to an existing customer.
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
    /// Current stage of the opportunity.
    /// </summary>
    public OpportunityStage Stage { get; set; }

    /// <summary>
    /// Expected deal value.
    /// </summary>
    public decimal ExpectedValue { get; set; }

    /// <summary>
    /// Expected close date (optional).
    /// </summary>
    public DateTime? ExpectedCloseDate { get; set; }

    /// <summary>
    /// Free-text notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Activities linked to this opportunity.
    /// </summary>
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();

    /// <summary>
    /// Follow-ups linked to this opportunity.
    /// </summary>
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
