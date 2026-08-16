using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Employees;

/// <summary>
/// DTO for displaying an employee.
/// </summary>
public class EmployeeDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public Guid DepartmentId { get; set; }
    public string? DepartmentNameAr { get; set; }
    public Guid PositionId { get; set; }
    public string? PositionNameAr { get; set; }
    public DateTime HireDate { get; set; }
    public EmployeeStatus Status { get; set; }
    public decimal BasicSalary { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}
