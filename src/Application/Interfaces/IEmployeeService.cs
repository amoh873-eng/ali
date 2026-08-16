using ERPSystem.Application.DTOs.Employees;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for employee operations (الموظفون).
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Returns all employees (with their department and position names).
    /// </summary>
    Task<List<EmployeeDto>> GetAllAsync();

    /// <summary>
    /// Gets a single employee by id.
    /// </summary>
    Task<EmployeeDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new employee. Validates employee number uniqueness, department and position existence.
    /// </summary>
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);

    /// <summary>
    /// Updates an existing employee.
    /// </summary>
    Task<EmployeeDto> UpdateAsync(UpdateEmployeeDto dto);

    /// <summary>
    /// Soft-deletes an employee. Fails if the employee has leave requests.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Changes the employee lifecycle status (نشط/موقوف/منتهي الخدمة).
    /// </summary>
    Task<EmployeeDto> SetStatusAsync(Guid id, EmployeeStatus status);
}
