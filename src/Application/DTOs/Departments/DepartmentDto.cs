namespace ERPSystem.Application.DTOs.Departments;

/// <summary>
/// DTO for displaying an organizational department (used in tables and trees).
/// </summary>
public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public string? ParentDepartmentName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }

    /// <summary>
    /// Child departments for building the tree in the UI.
    /// </summary>
    public List<DepartmentDto> Children { get; set; } = new();
}
