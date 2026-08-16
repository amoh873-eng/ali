using ERPSystem.Application.DTOs.Crm;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for sales opportunity operations (الفرص البيعية).
/// </summary>
public interface IOpportunityService
{
    Task<List<OpportunityDto>> GetAllAsync();
    Task<OpportunityDto?> GetByIdAsync(Guid id);
    Task<OpportunityDto> CreateAsync(CreateOpportunityDto dto);
    Task<OpportunityDto> UpdateAsync(UpdateOpportunityDto dto);
    Task DeleteAsync(Guid id);
    Task<OpportunityDto> UpdateStageAsync(Guid id, int stage);
}
