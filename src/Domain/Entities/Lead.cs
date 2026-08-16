using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a lead (عميل محتمل) — a potential customer before becoming an actual Customer.
/// When converted, an existing Customer record is created and linked via ConvertedCustomerId.
/// </summary>
public class Lead : BaseEntity
{
    /// <summary>
    /// Auto-generated unique code (e.g., "LEAD-20260815-0001").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Lead name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Lead name in English (optional).
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Contact person name (جهة الاتصال).
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Where this lead came from.
    /// </summary>
    public LeadSource Source { get; set; }

    /// <summary>
    /// Current lifecycle status.
    /// </summary>
    public LeadStatus Status { get; set; }

    /// <summary>
    /// Free-text notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// The Customer created when this lead was converted (null until converted).
    /// </summary>
    public Guid? ConvertedCustomerId { get; set; }

    /// <summary>
    /// Navigation property to the converted customer.
    /// </summary>
    public Customer? ConvertedCustomer { get; set; }

    /// <summary>
    /// Opportunities linked to this lead.
    /// </summary>
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

    /// <summary>
    /// Activities linked to this lead.
    /// </summary>
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();

    /// <summary>
    /// Follow-ups linked to this lead.
    /// </summary>
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
