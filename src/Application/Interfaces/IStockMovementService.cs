using ERPSystem.Application.DTOs.Movements;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for stock movement operations (حركات المخزون).
/// Handles inbound/outbound movements, transfers, and balance reporting.
/// </summary>
public interface IStockMovementService
{
    /// <summary>
    /// Records a single stock movement and updates the item's CurrentStock.
    /// Validates warehouse balance for outbound movements.
    /// </summary>
    Task<StockMovementDto> CreateAsync(CreateStockMovementDto dto);

    /// <summary>
    /// Transfers stock between two warehouses atomically (TransferOut + TransferIn).
    /// </summary>
    Task<(StockMovementDto Out, StockMovementDto In)> CreateTransferAsync(TransferStockDto dto);

    /// <summary>
    /// Returns all movements ordered by date (newest first).
    /// </summary>
    Task<List<StockMovementDto>> GetMovementsAsync();

    /// <summary>
    /// Returns the movement history for a specific item.
    /// </summary>
    Task<List<StockMovementDto>> GetMovementsByItemAsync(Guid itemId);

    /// <summary>
    /// Returns the current stock balance per (item, warehouse) pair.
    /// </summary>
    Task<List<StockBalanceDto>> GetStockBalancesAsync();
}