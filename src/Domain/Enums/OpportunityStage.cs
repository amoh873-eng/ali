namespace ERPSystem.Domain.Enums;

/// <summary>
/// Stage of a sales opportunity (مرحلة الفرصة البيعية).
/// </summary>
public enum OpportunityStage
{
    /// <summary>استكشاف - Prospecting</summary>
    Prospecting = 1,

    /// <summary>تأهيل - Qualification</summary>
    Qualification = 2,

    /// <summary>عرض سعر - Proposal</summary>
    Proposal = 3,

    /// <summary>تفاوض - Negotiation</summary>
    Negotiation = 4,

    /// <summary>فاز - Won</summary>
    Won = 5,

    /// <summary>خسر - Lost</summary>
    Lost = 6
}
