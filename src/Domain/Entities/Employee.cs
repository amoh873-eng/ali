using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents an employee (موظف) in the organization.
/// Note: no IsActive flag — the employee state is three-fold (Status):
/// Active / Suspended / Terminated. BasicSalary is kept here to be consumed
/// by a future payroll module.
/// </summary>
public class Employee : BaseEntity
{
    /// <summary>
    /// Unique employee number (الرقم الوظيفي).
    /// </summary>
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Employee name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Employee name in English (optional).
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Foreign key to the department.
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Navigation property to the department.
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// Foreign key to the position (job title).
    /// </summary>
    public Guid PositionId { get; set; }

    /// <summary>
    /// Navigation property to the position.
    /// </summary>
    public Position? Position { get; set; }

    /// <summary>
    /// Hiring date (تاريخ التعيين).
    /// </summary>
    public DateTime HireDate { get; set; }

    /// <summary>
    /// Employee lifecycle status (نشط/موقوف/منتهي الخدمة).
    /// </summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>
    /// Basic salary (الراتب الأساسي) — consumed by a future payroll module.
    /// </summary>
    public decimal BasicSalary { get; set; }

    /// <summary>
    /// Phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Optional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Leave requests belonging to this employee.
    /// </summary>
    public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
}
