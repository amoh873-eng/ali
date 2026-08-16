using System.ComponentModel.DataAnnotations;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Leaves;

/// <summary>
/// DTO for creating a new leave request.
/// </summary>
public class CreateLeaveDto
{
    [Required(ErrorMessage = "الموظف مطلوب")]
    public Guid EmployeeId { get; set; }

    public LeaveType LeaveType { get; set; } = LeaveType.Annual;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Reason { get; set; }
}
