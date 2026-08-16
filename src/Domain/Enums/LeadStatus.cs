namespace ERPSystem.Domain.Enums;

/// <summary>
/// Lifecycle of a lead (عميل محتمل).
/// </summary>
public enum LeadStatus
{
    /// <summary>جديد - New</summary>
    New = 1,

    /// <summary>تم التواصل - Contacted</summary>
    Contacted = 2,

    /// <summary>مؤهّل - Qualified</summary>
    Qualified = 3,

    /// <summary>محوّل لعميل - Converted</summary>
    Converted = 4,

    /// <summary>مرفوض - Rejected</summary>
    Rejected = 5
}
