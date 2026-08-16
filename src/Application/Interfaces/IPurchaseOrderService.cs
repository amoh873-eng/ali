using ERPSystem.Application.DTOs.Purchases;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for purchase order operations (أوامر الشراء).
/// A purchase order is a non-posting document; the only side-effect happens at
/// conversion time, when a real (posted) purchase invoice is created from the lines.
/// </summary>
public interface IPurchaseOrderService
{
    /// <summary>
    /// Returns all orders (newest first) with supplier names.
    /// </summary>
    Task<List<PurchaseOrderDto>> GetOrdersAsync();

    /// <summary>
    /// Gets a single order including its lines.
    /// </summary>
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new order in Draft status. No stock or accounting effect.
    /// </summary>
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto);

    /// <summary>
    /// Marks a Draft order as Approved (ready to convert).
    /// </summary>
    Task ApproveAsync(Guid id);

    /// <summary>
    /// Cancels an order (only allowed for Draft/Approved orders).
    /// </summary>
    Task CancelAsync(Guid id);

    /// <summary>
    /// Converts an approved order into a posted purchase invoice.
    /// The invoice creation performs stock receipt + accounting integration.
    /// </summary>
    Task<PurchaseOrderDto> ConvertToInvoiceAsync(Guid id, Guid warehouseId, int invoiceType);
}