using ERPSystem.Application.DTOs.Crm;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements lead business logic (العملاء المحتملون), including the conversion
/// of a lead into an actual Customer.
/// </summary>
public class LeadService : ILeadService
{
    private readonly DbContext _context;

    public LeadService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<LeadDto>> GetAllAsync()
    {
        var leads = await _context.Set<Lead>()
            .Include(l => l.ConvertedCustomer)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return leads.Select(MapToDto).ToList();
    }

    public async Task<LeadDto?> GetByIdAsync(Guid id)
    {
        var lead = await _context.Set<Lead>()
            .Include(l => l.ConvertedCustomer)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        return lead is null ? null : MapToDto(lead);
    }

    public async Task<LeadDto> CreateAsync(CreateLeadDto dto)
    {
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            Code = await NumberSequenceHelper.NextAsync(_context, "LEAD"),
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            ContactPerson = dto.ContactPerson,
            Phone = dto.Phone,
            Email = dto.Email,
            Source = (LeadSource)dto.Source,
            Status = LeadStatus.New,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Lead>().Add(lead);
        await _context.SaveChangesAsync();

        return MapToDto(lead);
    }

    public async Task<LeadDto> UpdateAsync(UpdateLeadDto dto)
    {
        var lead = await _context.Set<Lead>()
            .Include(l => l.ConvertedCustomer)
            .FirstOrDefaultAsync(l => l.Id == dto.Id && !l.IsDeleted);

        if (lead is null)
            throw new InvalidOperationException("العميل المحتمل غير موجود");

        lead.NameAr = dto.NameAr;
        lead.NameEn = dto.NameEn;
        lead.ContactPerson = dto.ContactPerson;
        lead.Phone = dto.Phone;
        lead.Email = dto.Email;
        lead.Source = (LeadSource)dto.Source;
        lead.Status = (LeadStatus)dto.Status;
        lead.Notes = dto.Notes;
        lead.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(lead);
    }

    public async Task DeleteAsync(Guid id)
    {
        var lead = await _context.Set<Lead>()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (lead is null)
            throw new InvalidOperationException("العميل المحتمل غير موجود");

        if (lead.Status == LeadStatus.Converted)
            throw new InvalidOperationException("لا يمكن حذف عميل محتمل تم تحويله لعميل");

        lead.IsDeleted = true;
        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<LeadDto> ConvertToCustomerAsync(ConvertLeadDto dto)
    {
        var lead = await _context.Set<Lead>()
            .Include(l => l.ConvertedCustomer)
            .FirstOrDefaultAsync(l => l.Id == dto.LeadId && !l.IsDeleted);

        if (lead is null)
            throw new InvalidOperationException("العميل المحتمل غير موجود");

        if (lead.Status == LeadStatus.Converted || lead.ConvertedCustomerId.HasValue)
            throw new InvalidOperationException("هذا العميل المحتمل محوّل مسبقاً");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Code = await NumberSequenceHelper.NextAsync(_context, "C"),
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            TaxNumber = dto.TaxNumber,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Customer>().Add(customer);

        lead.ConvertedCustomerId = customer.Id;
        lead.ConvertedCustomer = customer;
        lead.Status = LeadStatus.Converted;
        lead.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(lead);
    }

    private static LeadDto MapToDto(Lead lead) => new()
    {
        Id = lead.Id,
        Code = lead.Code,
        NameAr = lead.NameAr,
        NameEn = lead.NameEn,
        ContactPerson = lead.ContactPerson,
        Phone = lead.Phone,
        Email = lead.Email,
        Source = (int)lead.Source,
        Status = (int)lead.Status,
        Notes = lead.Notes,
        ConvertedCustomerId = lead.ConvertedCustomerId,
        ConvertedCustomerName = lead.ConvertedCustomer?.NameAr
    };
}
