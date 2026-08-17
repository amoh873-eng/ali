using ERPSystem.Application.DTOs.Movements;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements stock movement business logic (حركات المخزون).
///
/// شرح تصميم الإشارة (Sign):
/// - المستخدم يدخل كمية موجبة دائماً.
/// - الخدمة تحدد الإشارة تلقائياً من نوع الحركة:
///   واردة (PurchaseReceipt, OpeningBalance, AdjustmentIn, TransferIn) → +
///   صادرة (SalesIssue, AdjustmentOut, TransferOut) → -
/// - الحركات الصادرة تتحقق من كفاية الرصيد قبل التنفيذ.
/// - Item.CurrentStock يُحدَّث تلقائياً مع كل حركة (حقل مكرر للأداء).
/// </summary>
public partial class StockMovementService : IStockMovementService
{
    private readonly DbContext _context;

    public StockMovementService(DbContext context)
    {
        _context = context;
    }

    public async Task<StockMovementDto> CreateAsync(CreateStockMovementDto dto)
    {
        var item = await _context.Set<Item>()
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId && !i.IsDeleted);
        if (item is null)
            throw new InvalidOperationException("الصنف غير موجود");

        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted);
        if (warehouse is null)
            throw new InvalidOperationException("المخزن غير موجود");

        var type = (MovementType)dto.MovementType;
        var signedQuantity = GetSign(type) * dto.Quantity;

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // الحركات الصادرة تتطلب رصيداً كافياً
            if (signedQuantity < 0)
                await StockAvailabilityHelper.EnsureEnoughStockAsync(_context, dto.ItemId, dto.WarehouseId, dto.Quantity);

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ItemId = dto.ItemId,
            WarehouseId = dto.WarehouseId,
            MovementType = type,
            Quantity = signedQuantity,
            UnitCost = dto.UnitCost,
            ReferenceNumber = dto.ReferenceNumber,
            Note = dto.Note,
            MovementDate = dto.MovementDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<StockMovement>().Add(movement);

        // تقييم المتوسط المرجح للحركات الواردة (تكلفة الوحدة > 0)
        if (signedQuantity > 0 && dto.UnitCost > 0)
        {
            item.CostPrice = InventoryValuation.ComputeWeightedAverage(
                item.CurrentStock, item.CostPrice, dto.Quantity, dto.UnitCost);
        }

        // تحديث الرصيد الإجمالي للصنف
        item.CurrentStock += signedQuantity;
        item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return MapToDto(movement, item.Code, item.NameAr, warehouse.NameAr);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException("تعارض في تحديث بيانات المخزون — حاول مرة أخرى.");
        }
    }

    public async Task<(StockMovementDto Out, StockMovementDto In)> CreateTransferAsync(TransferStockDto dto)
    {
        if (dto.SourceWarehouseId == dto.DestinationWarehouseId)
            throw new InvalidOperationException("لا يمكن التحويل إلى نفس المخزن");

        var item = await _context.Set<Item>()
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId && !i.IsDeleted);
        if (item is null)
            throw new InvalidOperationException("الصنف غير موجود");

        var source = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == dto.SourceWarehouseId && !w.IsDeleted);
        if (source is null)
            throw new InvalidOperationException("مخزن المصدر غير موجود");

        var destination = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == dto.DestinationWarehouseId && !w.IsDeleted);
        if (destination is null)
            throw new InvalidOperationException("مخزن الوجهة غير موجود");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            await StockAvailabilityHelper.EnsureEnoughStockAsync(_context, dto.ItemId, dto.SourceWarehouseId, dto.Quantity);

        var reference = string.IsNullOrWhiteSpace(dto.ReferenceNumber)
            ? $"TR-{DateTime.Now:yyyyMMddHHmmss}"
            : dto.ReferenceNumber;

        // حركة خروج من المخزن المصدر + حركة دخول للمخزن الوجهة (ذرية)
        var outMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ItemId = dto.ItemId,
            WarehouseId = dto.SourceWarehouseId,
            MovementType = MovementType.TransferOut,
            Quantity = -dto.Quantity,
            UnitCost = item.CostPrice,
            ReferenceNumber = reference,
            Note = dto.Note,
            MovementDate = dto.MovementDate,
            CreatedAt = DateTime.UtcNow
        };

        var inMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ItemId = dto.ItemId,
            WarehouseId = dto.DestinationWarehouseId,
            MovementType = MovementType.TransferIn,
            Quantity = dto.Quantity,
            UnitCost = item.CostPrice,
            ReferenceNumber = reference,
            Note = dto.Note,
            MovementDate = dto.MovementDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<StockMovement>().AddRange(outMovement, inMovement);
        // الرصيد الإجمالي للصنف لا يتغير في التحويلات (صفر صافي)

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return (
                MapToDto(outMovement, item.Code, item.NameAr, source.NameAr),
                MapToDto(inMovement, item.Code, item.NameAr, destination.NameAr));
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException("تعارض في تحديث بيانات المخزون — حاول مرة أخرى.");
        }
    }
}
