using System.ComponentModel.DataAnnotations;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Employees;

/// <summary>
/// DTO for updating an existing employee.
/// </summary>
public class UpdateEmployeeDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "الرقم الوظيفي مطلوب")]
    [StringLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الموظف بالعربية مطلوب")]
    [StringLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEn { get; set; }

    [Required(ErrorMessage = "القسم مطلوب")]
    public Guid DepartmentId { get; set; }

    [Required(ErrorMessage = "المسمى الوظيفي مطلوب")]
    public Guid PositionId { get; set; }

    public DateTime HireDate { get; set; }

    public EmployeeStatus Status { get; set; }

    [Range(0, 10000000, ErrorMessage = "الراتب غير صالح")]
    public decimal BasicSalary { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
