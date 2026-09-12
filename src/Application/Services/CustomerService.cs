using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements customer business logic (العملاء).
/// Rules: الكود فريد، لا يمكن حذف عميل له مستندات مرحّلة،
/// وكشف حساب العميل يبني الرصيد الجاري من الفواتير والمردودات.
/// </summary>
public partial class CustomerService : ICustomerService
{
    private readonly DbContext _context;

    public CustomerService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        var customers = await _context.Set<Customer>()
            .OrderBy(c => c.Code)
            .ToListAsync();

        return customers.Select(MapToDto).ToList();
    }

    public async Task<List<CustomerDto>> SearchAsync(string search)
    {
        var query = _context.Set<Customer>().Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            // PostgreSQL: ILike = مطابقة نصية غير حساسة لحالة الأحرف (تم تحويلها من Contains/LIKE
            // لأن LIKE في PostgreSQL حساس للحالة — وهذا كان سيفشل صامتاً مع باركودات/أسماء لاتينية).
            query = query.Where(c =>
                EF.Functions.ILike(c.Code, $"%{term}%") ||
                EF.Functions.ILike(c.NameAr, $"%{term}%") ||
                (c.NameEn != null && EF.Functions.ILike(c.NameEn, $"%{term}%")));
        }

        var customers = await query.OrderBy(c => c.Code).ToListAsync();
        return customers.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var customer = await _context.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        return customer is null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        var codeExists = await _context.Set<Customer>()
            .AnyAsync(c => c.Code == dto.Code && !c.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود العميل '{dto.Code}' موجود مسبقاً");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            NameAr = dto.NameAr,
            NameEn = dto.NameEn,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            TaxNumber = dto.TaxNumber,
            CreditLimit = dto.CreditLimit,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Customer>().Add(customer);
        await _context.SaveChangesAsync();

        return MapToDto(customer);
    }
}