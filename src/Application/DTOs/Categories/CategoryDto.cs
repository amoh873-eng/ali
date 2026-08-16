namespace ERPSystem.Application.DTOs.Categories;

/// <summary>
/// Data Transfer Object for displaying an item category (used in tables and trees).
/// </summary>
public class CategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }

    /// <summary>
    /// Number of items directly assigned to this category.
    /// </summary>
    public int ItemsCount { get; set; }

    /// <summary>
    /// Child categories for building the tree in the UI.
    /// </summary>
    public List<CategoryDto> Children { get; set; } = new();
}