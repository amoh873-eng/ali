using ERPSystem.Application.DTOs.Warehouses;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for warehouse operations (المخازن).
/// </summary>
public interface IWarehouseService
{
    /// <summary>
    /// Returns all active warehouses.
    /// </summary>
    Task<List<WarehouseDto>> GetAllAsync();

    /// <summary>
    /// Gets a single warehouse by id.
    /// </summary>
    Task<WarehouseDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new warehouse. Validates code uniqueness.
    /// </summary>
    Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto);

    /// <summary>
    /// Updates an existing warehouse.
    /// </summary>
    Task<WarehouseDto> UpdateAsync(UpdateWarehouseDto dto);

    /// <summary>
    /// Soft-deletes a warehouse. Fails for system warehouses or ones with movements.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggles the active status of a warehouse.
    /// </summary>
    Task ToggleActiveAsync(Guid id);

    /// <summary>
    /// Marks a warehouse as the default (only one default is allowed).
    /// </summary>
    Task SetDefaultAsync(Guid id);
}