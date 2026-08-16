using ERPSystem.Application.DTOs.Categories;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements item category business logic (فئات الأصناف).
/// Similar to the Chart of Accounts tree: categories support multiple levels
/// through a self-referencing parent relationship.
/// </summary>
public partial class CategoryService : ICategoryService
{
    public async Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto)
    {
        var category = await _context.Set<Category>()
            .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted);

        if (category is null)
            throw new InvalidOperationException("الفئة غير موجودة");

        if (category.IsSystem)
            throw new InvalidOperationException("لا يمكن تعديل الفئات النظامية");

        var codeExists = await _context.Set<Category>()
            .AnyAsync(c => c.Code == dto.Code && c.Id != dto.Id && !c.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود الفئة '{dto.Code}' موجود مسبقاً");

        if (dto.ParentCategoryId == dto.Id)
            throw new InvalidOperationException("لا يمكن أن تكون الفئة أباً لنفسها");

        category.Code = dto.Code;
        category.NameAr = dto.NameAr;
        category.NameEn = dto.NameEn;
        category.ParentCategoryId = dto.ParentCategoryId;
        category.Description = dto.Description;
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _context.Set<Category>()
            .Include(c => c.ChildCategories)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (category is null)
            throw new InvalidOperationException("الفئة غير موجودة");

        if (category.IsSystem)
            throw new InvalidOperationException("لا يمكن حذف الفئات النظامية");

        var activeChildren = category.ChildCategories.Where(c => !c.IsDeleted).ToList();
        if (activeChildren.Any())
            throw new InvalidOperationException(
                $"لا يمكن حذف الفئة لأنها تحتوي على {activeChildren.Count} فئة فرعية. احذف الفئات الفرعية أولاً.");

        if (category.Items.Count > 0)
            throw new InvalidOperationException(
                $"لا يمكن حذف الفئة لأنها مرتبطة بـ {category.Items.Count} صنف.");

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var category = await _context.Set<Category>()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (category is null)
            throw new InvalidOperationException("الفئة غير موجودة");

        category.IsActive = !category.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ==================== Helper Methods ====================

    private List<CategoryDto> BuildTree(List<Category> allCategories, Guid? parentId, int level = 0)
    {
        return allCategories
            .Where(c => c.ParentCategoryId == parentId)
            .Select(c =>
            {
                var dto = MapToDto(c);
                dto.Children = BuildTree(allCategories, c.Id, level + 1);
                return dto;
            })
            .ToList();
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Code = category.Code,
            NameAr = category.NameAr,
            NameEn = category.NameEn,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = category.ParentCategory?.NameAr,
            Description = category.Description,
            IsActive = category.IsActive,
            IsSystem = category.IsSystem,
            ItemsCount = category.Items.Count(i => !i.IsDeleted)
        };
    }
}