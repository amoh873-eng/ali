using ERPSystem.Application.DTOs.Crm;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for CRM activity operations (سجل التواصل).
/// </summary>
public interface IActivityService
{
    Task<List<ActivityDto>> GetAllAsync(Guid? customerId = null, Guid? leadId = null, Guid? opportunityId = null, int? type = null);
    Task<ActivityDto> CreateAsync(CreateActivityDto dto);
    Task DeleteAsync(Guid id);
}
