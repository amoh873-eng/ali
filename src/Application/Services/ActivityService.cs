using ERPSystem.Application.DTOs.Crm;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements CRM activity business logic (سجل التواصل).
/// </summary>
public class ActivityService : IActivityService
{
    private readonly DbContext _context;

    public ActivityService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<ActivityDto>> GetAllAsync(Guid? customerId = null, Guid? leadId = null, Guid? opportunityId = null, int? type = null)
    {
        var query = _context.Set<Activity>()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Opportunity)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(a => a.CustomerId == customerId.Value);

        if (leadId.HasValue)
            query = query.Where(a => a.LeadId == leadId.Value);

        if (opportunityId.HasValue)
            query = query.Where(a => a.OpportunityId == opportunityId.Value);

        if (type.HasValue)
            query = query.Where(a => a.Type == (ActivityType)type.Value);

        var list = await query
            .OrderByDescending(a => a.ActivityDate)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<ActivityDto> CreateAsync(CreateActivityDto dto)
    {
        if (!dto.CustomerId.HasValue && !dto.LeadId.HasValue && !dto.OpportunityId.HasValue)
            throw new InvalidOperationException("يجب ربط النشاط بعميل أو عميل محتمل أو فرصة");

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            Type = (ActivityType)dto.Type,
            ActivityDate = dto.ActivityDate,
            Description = dto.Description,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Activity>().Add(activity);
        await _context.SaveChangesAsync();

        return MapToDto(activity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var activity = await _context.Set<Activity>()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (activity is null)
            throw new InvalidOperationException("النشاط غير موجود");

        activity.IsDeleted = true;
        activity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static ActivityDto MapToDto(Activity activity) => new()
    {
        Id = activity.Id,
        Type = (int)activity.Type,
        ActivityDate = activity.ActivityDate,
        Description = activity.Description,
        CustomerId = activity.CustomerId,
        CustomerName = activity.Customer?.NameAr,
        LeadId = activity.LeadId,
        LeadName = activity.Lead?.NameAr,
        OpportunityId = activity.OpportunityId,
        OpportunityTitle = activity.Opportunity?.Title
    };
}
