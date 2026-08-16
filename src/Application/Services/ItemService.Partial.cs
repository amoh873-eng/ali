using ERPSystem.Application.DTOs.Items;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements inventory item business logic (الأصناف) — الجزء الثاني.
/// </summary>
public partial class ItemService : IItemService
{
    public async Task<ItemDto> UpdateAsync(UpdateItemDto dto)
    {
        var item = await _context.Set<Item>()
            .FirstOrDefaultAsync(i => i.Id == dto.Id && !i.IsDeleted);

        if (item is null)
            throw new InvalidOperationException("الصنف غير موجود");

        var codeExists = await _context.Set<Item>()
            .AnyAsync(i => i.Code == dto.Code && i.Id != dto.Id && !i.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود الصنف '{dto.Code}' موجود مسبقاً");

        var categoryExists = await _context.Set<Category>()
            .AnyAsync(c => c.Id == dto.CategoryId && !c.IsDeleted);
        if (!categoryExists)
            throw new InvalidOperationException("الفئة المحددة غير موجودة");

        var unitExists = await _context.Set<Unit>()
            .AnyAsync(u => u.Id == dto.UnitId && !u.IsDeleted);
        if (!unitExists)
            throw new InvalidOperationException("الوحدة المحددة غير موجودة");

        item.Code = dto.Code;
        item.NameAr = dto.NameAr;
        item.NameEn = dto.NameEn;
        item.CategoryId = dto.CategoryId;
        item.UnitId = dto.UnitId;
        item.CostPrice = dto.CostPrice;
        item.SalePrice = dto.SalePrice;
        item.MinStockLevel = dto.MinStockLevel;
        item.MaxStockLevel = dto.MaxStockLevel;
        item.Barcode = dto.Barcode;
        item.Description = dto.Description;
        item.IsActive = dto.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.Set<Item>()
            .Include(i => i.StockMovements)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (item is null)
            throw new InvalidOperationException("الصنف غير موجود");

        if (item.IsSystem)
            throw new InvalidOperationException("لا يمكن حذف الأصناف النظامية");

        if (item.StockMovements.Count > 0)
            throw new InvalidOperationException(
                "لا يمكن حذف الصنف لأنه يحتوي على حركات مخزون مسجلة.");

        item.IsDeleted = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var item = await _context.Set<Item>()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (item is null)
            throw new InvalidOperationException("الصنف غير موجود");

        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ==================== Helper Methods ====================

    private static ItemDto MapToDto(Item item)
    {
        return new ItemDto
        {
            Id = item.Id,
            Code = item.Code,
            NameAr = item.NameAr,
            NameEn = item.NameEn,
            CategoryId = item.CategoryId,
            CategoryNameAr = item.Category?.NameAr ?? "—",
            UnitId = item.UnitId,
            UnitNameAr = item.Unit?.NameAr ?? "—",
            CostPrice = item.CostPrice,
            SalePrice = item.SalePrice,
            MinStockLevel = item.MinStockLevel,
            MaxStockLevel = item.MaxStockLevel,
            Barcode = item.Barcode,
            Description = item.Description,
            IsActive = item.IsActive,
            IsSystem = item.IsSystem,
            CurrentStock = item.CurrentStock
        };
    }
}