using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Leaves;

/// <summary>
/// DTO for displaying a leave request.
/// </summary>
public class LeaveDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeNameAr { get; set; }
    public string? EmployeeNumber { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LeaveStatus Status { get; set; }
    public string? Reason { get; set; }
}
