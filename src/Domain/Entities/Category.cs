using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents an item category (فئة الأصناف).
/// Supports a multi-level tree through self-referencing (ParentCategory),
/// exactly like the Chart of Accounts tree: فئة رئيسية → فئة فرعية → ...
/// </summary>
public class Category : BaseEntity
{
    /// <summary>
    /// Unique category code (e.g., "RAW", "FIN", "MAT").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Category name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Category name in English.
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Foreign key to the parent category (null for root categories).
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>
    /// Navigation property to the parent category.
    /// </summary>
    public Category? ParentCategory { get; set; }

    /// <summary>
    /// Child categories forming the tree structure.
    /// </summary>
    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this category is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system category that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Items belonging to this category.
    /// </summary>
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
