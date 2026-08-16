using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents an organizational department (قسم).
/// Supports a tree structure through self-referencing (ParentDepartment),
/// exactly like the Chart of Accounts and item categories.
/// </summary>
public class Department : BaseEntity
{
    /// <summary>
    /// Unique department code (e.g., "HR", "ACC").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Department name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Department name in English (optional).
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Foreign key to the parent department (null for root departments).
    /// </summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>
    /// Navigation property to the parent department.
    /// </summary>
    public Department? ParentDepartment { get; set; }

    /// <summary>
    /// Child departments forming the tree.
    /// </summary>
    public ICollection<Department> ChildDepartments { get; set; } = new List<Department>();

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this department is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system department that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Positions belonging to this department.
    /// </summary>
    public ICollection<Position> Positions { get; set; } = new List<Position>();

    /// <summary>
    /// Employees belonging to this department.
    /// </summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
