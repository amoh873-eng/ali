using ERPSystem.Application.DTOs.Sales;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for sales quote operations (عروض الأسعار).
/// A quote is a non-posting document; the only side-effect happens at conversion
/// time, when a real (posted) invoice is created from the quote lines.
/// </summary>
public interface ISalesQuoteService
{
    /// <summary>
    /// Returns all quotes (newest first) with customer names.
    /// </summary>
    Task<List<SalesQuoteDto>> GetQuotesAsync();

    /// <summary>
    /// Gets a single quote including its lines.
    /// </summary>
    Task<SalesQuoteDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new quote in Draft status. No stock or accounting effect.
    /// </summary>
    Task<SalesQuoteDto> CreateAsync(CreateSalesQuoteDto dto);

    /// <summary>
    /// Marks a Draft quote as Approved (ready to convert).
    /// </summary>
    Task ApproveAsync(Guid id);

    /// <summary>
    /// Cancels a quote (only allowed for Draft/Approved quotes).
    /// </summary>
    Task CancelAsync(Guid id);

    /// <summary>
    /// Converts an approved quote into a posted sales invoice.
    /// The invoice creation performs stock issue + accounting integration.
    /// </summary>
    Task<SalesQuoteDto> ConvertToInvoiceAsync(Guid id, Guid warehouseId, int invoiceType);
}