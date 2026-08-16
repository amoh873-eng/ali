using ERPSystem.Application.DTOs.Crm;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for scheduled follow-up operations (المتابعات المجدولة).
/// </summary>
public interface IFollowUpService
{
    Task<List<FollowUpDto>> GetBySubjectAsync(Guid? customerId = null, Guid? leadId = null, Guid? opportunityId = null);
    Task<FollowUpDto> CreateAsync(CreateFollowUpDto dto);
    Task<FollowUpDto> MarkDoneAsync(Guid id);
    Task DeleteAsync(Guid id);
}
