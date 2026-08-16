using ERPSystem.Application.DTOs.Items;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for inventory item operations (الأصناف).
/// </summary>
public interface IItemService
{
    /// <summary>
    /// Returns all active items with category and unit names.
    /// </summary>
    Task<List<ItemDto>> GetAllAsync();

    /// <summary>
    /// Returns items whose current stock is below the minimum level.
    /// </summary>
    Task<List<ItemDto>> GetLowStockAsync();

    /// <summary>
    /// Gets a single item by id.
    /// </summary>
    Task<ItemDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new item. Validates code uniqueness, category and unit existence.
    /// </summary>
    Task<ItemDto> CreateAsync(CreateItemDto dto);

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    Task<ItemDto> UpdateAsync(UpdateItemDto dto);

    /// <summary>
    /// Soft-deletes an item. Fails for system items or items with movements.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggles the active status of an item.
    /// </summary>
    Task ToggleActiveAsync(Guid id);
}