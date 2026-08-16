using ERPSystem.Application.DTOs.Positions;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for job position operations (المسميات الوظيفية).
/// </summary>
public interface IPositionService
{
    /// <summary>
    /// Returns all positions (with their department names).
    /// </summary>
    Task<List<PositionDto>> GetAllAsync();

    /// <summary>
    /// Gets a single position by id.
    /// </summary>
    Task<PositionDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new position. Validates code uniqueness and department existence.
    /// </summary>
    Task<PositionDto> CreateAsync(CreatePositionDto dto);

    /// <summary>
    /// Updates an existing position.
    /// </summary>
    Task<PositionDto> UpdateAsync(UpdatePositionDto dto);

    /// <summary>
    /// Soft-deletes a position. Fails if it is a system position or used by employees.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggles the active status of a position.
    /// </summary>
    Task ToggleActiveAsync(Guid id);
}
