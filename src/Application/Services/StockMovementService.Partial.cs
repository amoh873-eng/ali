using ERPSystem.Application.DTOs.Movements;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements stock movement business logic (حركات المخزون) — الجزء الثاني: الاستعلامات والمساعدات.
/// </summary>
public partial class StockMovementService : IStockMovementService
{
    public async Task<List<StockMovementDto>> GetMovementsAsync()
    {
        var movements = await _context.Set<StockMovement>()
            .Include(m => m.Item)
            .Include(m => m.Warehouse)
            .OrderByDescending(m => m.MovementDate)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        return movements
            .Select(m => MapToDto(m, m.Item?.Code ?? "—", m.Item?.NameAr ?? "—", m.Warehouse?.NameAr ?? "—"))
            .ToList();
    }

    public async Task<List<StockMovementDto>> GetMovementsByItemAsync(Guid itemId)
    {
        var movements = await _context.Set<StockMovement>()
            .Include(m => m.Item)
            .Include(m => m.Warehouse)
            .Where(m => m.ItemId == itemId)
            .OrderByDescending(m => m.MovementDate)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        return movements
            .Select(m => MapToDto(m, m.Item?.Code ?? "—", m.Item?.NameAr ?? "—", m.Warehouse?.NameAr ?? "—"))
            .ToList();
    }

    public async Task<List<StockBalanceDto>> GetStockBalancesAsync()
    {
        var balances = await _context.Set<StockMovement>()
            .Include(m => m.Item)
            .Include(m => m.Warehouse)
            .GroupBy(m => new { m.ItemId, m.WarehouseId })
            .Select(g => new StockBalanceDto
            {
                ItemId = g.Key.ItemId,
                ItemCode = g.First().Item!.Code,
                ItemNameAr = g.First().Item!.NameAr,
                WarehouseId = g.Key.WarehouseId,
                WarehouseNameAr = g.First().Warehouse!.NameAr,
                Quantity = g.Sum(m => m.Quantity),
                UnitCost = g.First().Item!.CostPrice
            })
            .Where(b => b.Quantity != 0)
            .OrderBy(b => b.ItemCode)
            .ThenBy(b => b.WarehouseNameAr)
            .ToListAsync();

        return balances;
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// Checks that the available stock of (item, warehouse) is enough for an outbound movement.
    /// </summary>
    private async Task EnsureEnoughStockAsync(Guid itemId, Guid warehouseId, decimal quantity)
    {
        var available = await _context.Set<StockMovement>()
            .Where(m => m.ItemId == itemId && m.WarehouseId == warehouseId)
            .SumAsync(m => (decimal?)m.Quantity) ?? 0m;

        if (available < quantity)
            throw new InvalidOperationException(
                $"الرصيد غير كافٍ. المتاح: {available:N0}، المطلوب: {quantity:N0}.");
    }

    /// <summary>
    /// Determines the sign of a movement from its type (+ inbound, - outbound).
    /// </summary>
    private static int GetSign(MovementType type)
    {
        return type switch
        {
            MovementType.PurchaseReceipt or MovementType.OpeningBalance
                or MovementType.AdjustmentIn or MovementType.TransferIn
                or MovementType.SalesReturnIn => 1,
            _ => -1
        };
    }

    private static StockMovementDto MapToDto(StockMovement m, string itemCode, string itemNameAr, string warehouseNameAr)
    {
        return new StockMovementDto
        {
            Id = m.Id,
            ItemId = m.ItemId,
            ItemCode = itemCode,
            ItemNameAr = itemNameAr,
            WarehouseId = m.WarehouseId,
            WarehouseNameAr = warehouseNameAr,
            MovementType = (int)m.MovementType,
            MovementTypeNameAr = GetMovementTypeNameAr(m.MovementType),
            Quantity = m.Quantity,
            UnitCost = m.UnitCost,
            ReferenceNumber = m.ReferenceNumber,
            Note = m.Note,
            MovementDate = m.MovementDate
        };
    }

    private static string GetMovementTypeNameAr(MovementType type)
    {
        return type switch
        {
            MovementType.PurchaseReceipt => "وارد مشتريات",
            MovementType.SalesIssue => "صادر مبيعات",
            MovementType.OpeningBalance => "رصيد افتتاحي",
            MovementType.AdjustmentIn => "تسوية إضافة",
            MovementType.AdjustmentOut => "تسوية خصم",
            MovementType.TransferIn => "وارد تحويل",
            MovementType.TransferOut => "صادر تحويل",
            MovementType.SalesReturnIn => "وارد مردود مبيعات",
            _ => "غير معروف"
        };
    }
}