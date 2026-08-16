using ERPSystem.Application.DTOs.Movements;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements periodic stock count logic (الجرد الدوري).
///
/// الفكرة: نُحصي فعلياً كميات الأصناف في مخزن، ونقارنها بالأرصدة النظامية.
/// لكل فرق (زيادة أو عجز) نولّد حركة تسوية (AdjustmentIn/AdjustmentOut)
/// بتكلفة الوحدة الحالية، ونحدّث رصيد الصنف. لا يُعاد حساب المتوسط المرجح
/// هنا لأن التسوية تتم بتكلفة المتوسط نفسها فلا تغيّره.
/// </summary>
public class StockCountService : IStockCountService
{
    private readonly DbContext _context;

    public StockCountService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<StockCountDto>> GetCountsAsync()
    {
        var counts = await _context.Set<StockCount>()
            .Include(c => c.Warehouse)
            .Include(c => c.Lines)
            .OrderByDescending(c => c.CountDate)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();
        return counts.Select(MapToDto).ToList();
    }

    public async Task<StockCountDto?> GetByIdAsync(Guid id)
    {
        var count = await _context.Set<StockCount>()
            .Include(c => c.Warehouse)
            .Include(c => c.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        return count is null ? null : MapToDto(count);
    }

    public async Task<StockCountDto> CreateAsync(CreateStockCountDto dto)
    {
        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted);
        if (warehouse is null) throw new InvalidOperationException("المخزن غير موجود");

        var items = await _context.Set<Item>()
            .Where(i => !i.IsDeleted && i.IsActive)
            .ToListAsync();

        // الأرصدة النظامية الحالية لكل صنف في هذا المخزن
        var balances = await _context.Set<StockMovement>()
            .Where(m => m.WarehouseId == dto.WarehouseId)
            .GroupBy(m => m.ItemId)
            .Select(g => new { ItemId = g.Key, Quantity = g.Sum(m => m.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Quantity);

        var itemDict = items.ToDictionary(i => i.Id);
        var inputDict = dto.Lines
            .GroupBy(l => l.ItemId)
            .ToDictionary(g => g.Key, g => g.First().CountedQuantity);

        var count = new StockCount
        {
            Id = Guid.NewGuid(),
            StockCountNumber = await NextCountNumberAsync(),
            WarehouseId = warehouse.Id,
            CountDate = dto.CountDate,
            Status = DocumentStatus.Posted,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        var lines = new List<StockCountLine>();
        foreach (var item in items)
        {
            var systemQty = balances.GetValueOrDefault(item.Id, 0m);
            var countedQty = inputDict.GetValueOrDefault(item.Id, systemQty);
            lines.Add(new StockCountLine
            {
                Id = Guid.NewGuid(),
                StockCountId = count.Id,
                ItemId = item.Id,
                SystemQuantity = systemQty,
                CountedQuantity = countedQty,
                Difference = countedQty - systemQty,
                CreatedAt = DateTime.UtcNow
            });
        }
        count.Lines = lines;

        _context.Set<StockCount>().Add(count);
        _context.Set<StockCountLine>().AddRange(lines);

        // توليد حركات التسوية للفروقات وتحديث الأرصدة
        foreach (var line in lines)
        {
            if (line.Difference == 0) continue;

            var item = itemDict[line.ItemId];
            var type = line.Difference > 0 ? MovementType.AdjustmentIn : MovementType.AdjustmentOut;
            _context.Set<StockMovement>().Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ItemId = line.ItemId,
                WarehouseId = warehouse.Id,
                MovementType = type,
                Quantity = line.Difference,
                UnitCost = item.CostPrice,
                ReferenceNumber = count.StockCountNumber,
                Note = $"تسوية جرد {count.StockCountNumber}",
                MovementDate = count.CountDate,
                CreatedAt = DateTime.UtcNow
            });

            item.CurrentStock += line.Difference;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return MapToDto(count);
    }

    private Task<string> NextCountNumberAsync()
        => NumberSequenceHelper.NextAsync(_context, "SC");

    private static StockCountDto MapToDto(StockCount count)
    {
        return new StockCountDto
        {
            Id = count.Id,
            StockCountNumber = count.StockCountNumber,
            WarehouseId = count.WarehouseId,
            WarehouseName = count.Warehouse?.NameAr ?? "—",
            CountDate = count.CountDate,
            Status = (int)count.Status,
            Note = count.Note,
            LinesCount = count.Lines.Count,
            TotalDifference = count.Lines.Sum(l => l.Difference),
            Lines = count.Lines.Select(l => new StockCountLineDto
            {
                Id = l.Id,
                ItemId = l.ItemId,
                ItemCode = l.Item?.Code ?? "—",
                ItemNameAr = l.Item?.NameAr ?? "—",
                SystemQuantity = l.SystemQuantity,
                CountedQuantity = l.CountedQuantity,
                Difference = l.Difference
            }).ToList()
        };
    }
}