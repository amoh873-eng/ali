using ERPSystem.Application.DTOs.Crm;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements scheduled follow-up business logic (المتابعات المجدولة).
/// </summary>
public class FollowUpService : IFollowUpService
{
    private readonly DbContext _context;

    public FollowUpService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<FollowUpDto>> GetBySubjectAsync(Guid? customerId = null, Guid? leadId = null, Guid? opportunityId = null)
    {
        var query = _context.Set<FollowUp>().AsQueryable();

        if (customerId.HasValue)
            query = query.Where(f => f.CustomerId == customerId.Value);

        if (leadId.HasValue)
            query = query.Where(f => f.LeadId == leadId.Value);

        if (opportunityId.HasValue)
            query = query.Where(f => f.OpportunityId == opportunityId.Value);

        var list = await query
            .OrderBy(f => f.FollowUpDate)
            .ThenByDescending(f => f.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<FollowUpDto> CreateAsync(CreateFollowUpDto dto)
    {
        if (!dto.CustomerId.HasValue && !dto.LeadId.HasValue && !dto.OpportunityId.HasValue)
            throw new InvalidOperationException("يجب ربط المتابعة بعميل أو عميل محتمل أو فرصة");

        var followUp = new FollowUp
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            FollowUpDate = dto.FollowUpDate,
            Status = FollowUpStatus.Pending,
            Notes = dto.Notes,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<FollowUp>().Add(followUp);
        await _context.SaveChangesAsync();

        return MapToDto(followUp);
    }

    public async Task<FollowUpDto> MarkDoneAsync(Guid id)
    {
        var followUp = await _context.Set<FollowUp>()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (followUp is null)
            throw new InvalidOperationException("المتابعة غير موجودة");

        followUp.Status = FollowUpStatus.Done;
        followUp.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(followUp);
    }

    public async Task DeleteAsync(Guid id)
    {
        var followUp = await _context.Set<FollowUp>()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (followUp is null)
            throw new InvalidOperationException("المتابعة غير موجودة");

        followUp.IsDeleted = true;
        followUp.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static FollowUpDto MapToDto(FollowUp followUp) => new()
    {
        Id = followUp.Id,
        Title = followUp.Title,
        FollowUpDate = followUp.FollowUpDate,
        Status = (int)followUp.Status,
        Notes = followUp.Notes,
        CustomerId = followUp.CustomerId,
        LeadId = followUp.LeadId,
        OpportunityId = followUp.OpportunityId
    };
}
