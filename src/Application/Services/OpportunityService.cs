using ERPSystem.Application.DTOs.Crm;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements sales opportunity business logic (الفرص البيعية).
/// </summary>
public class OpportunityService : IOpportunityService
{
    private readonly DbContext _context;

    public OpportunityService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<OpportunityDto>> GetAllAsync()
    {
        var list = await _context.Set<Opportunity>()
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<OpportunityDto?> GetByIdAsync(Guid id)
    {
        var opportunity = await _context.Set<Opportunity>()
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        return opportunity is null ? null : MapToDto(opportunity);
    }

    public async Task<OpportunityDto> CreateAsync(CreateOpportunityDto dto)
    {
        ValidateSubject(dto.CustomerId, dto.LeadId);
        if (dto.ExpectedValue < 0)
            throw new InvalidOperationException("القيمة المتوقعة لا يمكن أن تكون سالبة");

        var opportunity = new Opportunity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Stage = (OpportunityStage)dto.Stage,
            ExpectedValue = Math.Round(dto.ExpectedValue, 2),
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Opportunity>().Add(opportunity);
        await _context.SaveChangesAsync();

        return MapToDto(opportunity);
    }

    public async Task<OpportunityDto> UpdateAsync(UpdateOpportunityDto dto)
    {
        var opportunity = await _context.Set<Opportunity>()
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .FirstOrDefaultAsync(o => o.Id == dto.Id && !o.IsDeleted);

        if (opportunity is null)
            throw new InvalidOperationException("الفرصة البيعية غير موجودة");

        ValidateSubject(dto.CustomerId, dto.LeadId);
        if (dto.ExpectedValue < 0)
            throw new InvalidOperationException("القيمة المتوقعة لا يمكن أن تكون سالبة");

        opportunity.Title = dto.Title;
        opportunity.CustomerId = dto.CustomerId;
        opportunity.LeadId = dto.LeadId;
        opportunity.Stage = (OpportunityStage)dto.Stage;
        opportunity.ExpectedValue = Math.Round(dto.ExpectedValue, 2);
        opportunity.ExpectedCloseDate = dto.ExpectedCloseDate;
        opportunity.Notes = dto.Notes;
        opportunity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(opportunity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var opportunity = await _context.Set<Opportunity>()
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (opportunity is null)
            throw new InvalidOperationException("الفرصة البيعية غير موجودة");

        opportunity.IsDeleted = true;
        opportunity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<OpportunityDto> UpdateStageAsync(Guid id, int stage)
    {
        if (stage < 1 || stage > 6)
            throw new InvalidOperationException("مرحلة غير صالحة");

        var opportunity = await _context.Set<Opportunity>()
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (opportunity is null)
            throw new InvalidOperationException("الفرصة البيعية غير موجودة");

        opportunity.Stage = (OpportunityStage)stage;
        opportunity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(opportunity);
    }

    private static void ValidateSubject(Guid? customerId, Guid? leadId)
    {
        if (!customerId.HasValue && !leadId.HasValue)
            throw new InvalidOperationException("يجب ربط الفرصة بعميل أو عميل محتمل");
    }

    private static OpportunityDto MapToDto(Opportunity opportunity) => new()
    {
        Id = opportunity.Id,
        Title = opportunity.Title,
        CustomerId = opportunity.CustomerId,
        CustomerName = opportunity.Customer?.NameAr,
        LeadId = opportunity.LeadId,
        LeadName = opportunity.Lead?.NameAr,
        Stage = (int)opportunity.Stage,
        ExpectedValue = opportunity.ExpectedValue,
        ExpectedCloseDate = opportunity.ExpectedCloseDate,
        Notes = opportunity.Notes
    };
}
