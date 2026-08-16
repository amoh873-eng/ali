namespace ERPSystem.Domain.Enums;

/// <summary>
/// Type of a CRM activity (نوع النشاط في سجل التواصل).
/// </summary>
public enum ActivityType
{
    /// <summary>مكالمة - Call</summary>
    Call = 1,

    /// <summary>اجتماع - Meeting</summary>
    Meeting = 2,

    /// <summary>بريد - Email</summary>
    Email = 3,

    /// <summary>ملاحظة - Note</summary>
    Note = 4
}
