namespace ERPSystem.Domain.Enums;

/// <summary>
/// Where a lead came from (مصدر العميل المحتمل).
/// </summary>
public enum LeadSource
{
    /// <summary>موقع إلكتروني - Website</summary>
    Website = 1,

    /// <summary>هاتف - Phone</summary>
    Phone = 2,

    /// <summary>إحالة - Referral</summary>
    Referral = 3,

    /// <summary>زيارة مباشرة - Walk-in</summary>
    WalkIn = 4,

    /// <summary>وسائل التواصل - Social media</summary>
    SocialMedia = 5,

    /// <summary>أخرى - Other</summary>
    Other = 6
}
