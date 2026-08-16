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
    private readonly DbContext _context;

    public CategoryService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> GetTreeAsync()
    {
        var categories = await _context.Set<Category>()
            .OrderBy(c => c.Code)
            .ToListAsync();

        return BuildTree(categories, null);
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _context.Set<Category>()
            .OrderBy(c => c.Code)
            .ToListAsync();

        return categories.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        var category = await _context.Set<Category>()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        return category is null ? null : MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var codeExists = await _context.Set<Category>()
            .AnyAsync(c => c.Code == dto.Code && !c.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود الفئة '{dto.Code}' موجود مسبقاً");

        if (dto.ParentCategoryId.HasValue)
        {
            var parentExists = await _context.Set<Category>()
                .AnyAsync(c => c.Id == dto.ParentCategoryId.Value && !c.IsDeleted);
            if (!parentExists)
                throw new InvalidOperationException("الفئة الأب غير موجودة");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            ParentCategoryId = dto.ParentCategoryId,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Category>().Add(category);
        await _context.SaveChangesAsync();

        return MapToDto(category);
    }
}
