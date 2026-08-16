using ERPSystem.Application.DTOs.Categories;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for item category operations (فئات الأصناف).
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Returns all categories as a hierarchical tree.
    /// </summary>
    Task<List<CategoryDto>> GetTreeAsync();

    /// <summary>
    /// Returns a flat list of all categories (for dropdowns).
    /// </summary>
    Task<List<CategoryDto>> GetAllAsync();

    /// <summary>
    /// Gets a single category by id.
    /// </summary>
    Task<CategoryDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new category. Validates code uniqueness and parent existence.
    /// </summary>
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto);

    /// <summary>
    /// Soft-deletes a category. Fails if it has children or assigned items.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Toggles the active status of a category.
    /// </summary>
    Task ToggleActiveAsync(Guid id);
}