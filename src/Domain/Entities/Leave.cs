using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a leave request (إجازة) for an employee — simplified version.
/// </summary>
public class Leave : BaseEntity
{
    /// <summary>
    /// Foreign key to the employee who owns this request.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Navigation property to the employee.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Leave type (سنوية/مرضية/بدون راتب/طارئة).
    /// </summary>
    public LeaveType LeaveType { get; set; }

    /// <summary>
    /// Start date of the leave.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the leave.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Leave request status (معلّقة/موافق عليها/مرفوضة).
    /// </summary>
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    /// <summary>
    /// Reason or note for the leave.
    /// </summary>
    public string? Reason { get; set; }
}
