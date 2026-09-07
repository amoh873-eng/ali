using ERPSystem.Application.DTOs.Items;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// ItemService — استعلامات شاشة ملصقات الباركود (مقسّمة / خادمية).
/// أُضيفت لإلغاء «اللود الضخم» الذي كان يجمّد الجهاز (تحميل كل الأصناف في الذاكرة
/// ثم رسمها كلها في الجدول) والذي كان يسبب أيضاً تعارض DbContext
/// ("A second operation was started on this context instance") أثناء فتح الصفحة والطباعة.
/// </summary>
public partial class ItemService
{
    private const int MaxSelectionIds = 20000; // حاجز أمان من الانفجار عند اختيار فئة/دفعة كاملة

    /// <summary>استعلام أساسي مشترك: أصناف غير محذوفة مع الفئة والوحدة.</summary>
    private IQueryable<Item> ActiveItemsQuery() =>
        _context.Set<Item>()
            .Include(i => i.Category)
            .Include(i => i.Unit)
            .Where(i => !i.IsDeleted);

    /// <inheritdoc />
    public async Task<ItemPageDto> SearchPageAsync(string? term, Guid? categoryId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var q = ActiveItemsQuery();

        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            q = q.Where(i => i.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            q = q.Where(i =>
                EF.Functions.ILike(i.Code, $"%{t}%") ||
                EF.Functions.ILike(i.NameAr, $"%{t}%") ||
                (i.NameEn != null && EF.Functions.ILike(i.NameEn, $"%{t}%")) ||
                (i.Barcode != null && EF.Functions.ILike(i.Barcode, $"%{t}%")));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(i => i.Code)
            .Skip(Math.Max(0, skip))
            .Take(Math.Max(1, take))
            .ToListAsync(cancellationToken);

        return new ItemPageDto
        {
            Items = items.Select(MapToDto).ToList(),
            Total = total
        };
    }

    /// <inheritdoc />
    public async Task<List<ItemDto>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Distinct().ToList() ?? new List<Guid>();
        if (idList.Count == 0) return new List<ItemDto>();

        var items = await ActiveItemsQuery()
            .Where(i => idList.Contains(i.Id))
            .OrderBy(i => i.Code)
            .ToListAsync();

        return items.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<Guid>> GetIdsByCategoryAsync(Guid categoryId)
    {
        if (categoryId == Guid.Empty) return new List<Guid>();

        return await _context.Set<Item>()
            .Where(i => !i.IsDeleted && i.CategoryId == categoryId)
            .OrderBy(i => i.Code)
            .Select(i => i.Id)
            .Take(MaxSelectionIds)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Guid>> GetLastBulkImportIdsAsync()
    {
        var marker = LastBulkImportCompletedAtUtc;
        if (marker is null) return new List<Guid>();

        return await _context.Set<Item>()
            .Where(i => !i.IsDeleted && i.CreatedAt >= marker)
            .OrderBy(i => i.Code)
            .Select(i => i.Id)
            .Take(MaxSelectionIds)
            .ToListAsync();
    }
}