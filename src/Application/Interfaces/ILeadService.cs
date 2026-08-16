using ERPSystem.Application.DTOs.Crm;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for lead operations (العملاء المحتملون).
/// </summary>
public interface ILeadService
{
    Task<List<LeadDto>> GetAllAsync();
    Task<LeadDto?> GetByIdAsync(Guid id);
    Task<LeadDto> CreateAsync(CreateLeadDto dto);
    Task<LeadDto> UpdateAsync(UpdateLeadDto dto);
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Converts a lead into an actual Customer: creates the Customer, links it back
    /// to the lead, and marks the lead as Converted (atomic).
    /// </summary>
    Task<LeadDto> ConvertToCustomerAsync(ConvertLeadDto dto);
}
