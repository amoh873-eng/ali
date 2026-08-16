using ERPSystem.Application.DTOs.Movements;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for periodic stock count operations (الجرد الدوري).
/// </summary>
public interface IStockCountService
{
    /// <summary>
    /// Returns all stock counts (newest first) with warehouse names.
    /// </summary>
    Task<List<StockCountDto>> GetCountsAsync();

    /// <summary>
    /// Gets a single count including its lines.
    /// </summary>
    Task<StockCountDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a stock count and immediately posts adjustment movements
    /// for the differences (atomic).
    /// </summary>
    Task<StockCountDto> CreateAsync(CreateStockCountDto dto);
}