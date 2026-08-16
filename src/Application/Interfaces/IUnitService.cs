using ERPSystem.Application.DTOs.Units;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for unit-of-measure operations (وحدات القياس).
/// </summary>
public interface IUnitService
{
    /// <summary>
    /// Returns all active units.
    /// </summary>
    Task<List<UnitDto>> GetAllAsync();

    /// <summary>
    /// Gets a single unit by id.
    /// </summary>
    Task<UnitDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new unit. Validates code uniqueness.
    /// </summary>
    Task<UnitDto> CreateAsync(CreateUnitDto dto);

    /// <summary>
    /// Updates an existing unit.
    /// </summary>
    Task<UnitDto> UpdateAsync(UpdateUnitDto dto);

    /// <summary>
    /// Soft-deletes a unit. Fails if it is a system unit or used by items.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggles the active status of a unit.
    /// </summary>
    Task ToggleActiveAsync(Guid id);
}