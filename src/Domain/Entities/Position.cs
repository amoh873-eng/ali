using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a job position / title (مسمى وظيفي) linked to a department.
/// </summary>
public class Position : BaseEntity
{
    /// <summary>
    /// Unique position code (e.g., "ACC-MGR").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Position name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Position name in English (optional).
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Foreign key to the department this position belongs to.
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Navigation property to the department.
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// Whether this position is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system position that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Employees holding this position.
    /// </summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
