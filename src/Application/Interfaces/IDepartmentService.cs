using ERPSystem.Application.DTOs.Departments;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for organizational department operations (الأقسام).
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// Returns all departments as a hierarchical tree.
    /// </summary>
    Task<List<DepartmentDto>> GetTreeAsync();

    /// <summary>
    /// Returns a flat list of all departments (for dropdowns).
    /// </summary>
    Task<List<DepartmentDto>> GetAllAsync();

    /// <summary>
    /// Gets a single department by id.
    /// </summary>
    Task<DepartmentDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new department. Validates code uniqueness and parent existence.
    /// </summary>
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);

    /// <summary>
    /// Updates an existing department.
    /// </summary>
    Task<DepartmentDto> UpdateAsync(UpdateDepartmentDto dto);

    /// <summary>
    /// Soft-deletes a department. Fails if it has children, positions or employees.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggles the active status of a department.
    /// </summary>
    Task ToggleActiveAsync(Guid id);
}
