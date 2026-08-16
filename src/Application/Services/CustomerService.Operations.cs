using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// CustomerService — الجزء الثاني: التعديل/الحذف والتفعيل وكشف الحساب.
/// </summary>
public partial class CustomerService
{
    public async Task<CustomerDto> UpdateAsync(UpdateCustomerDto dto)
    {
        var customer = await _context.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted);
        if (customer is null)
            throw new InvalidOperationException("العميل غير موجود");

        var codeExists = await _context.Set<Customer>()
            .AnyAsync(c => c.Code == dto.Code && c.Id != dto.Id && !c.IsDeleted);
        if (codeExists)
            throw new InvalidOperationException($"كود العميل '{dto.Code}' موجود مسبقاً");

        customer.Code = dto.Code;
        customer.NameAr = dto.NameAr;
        customer.NameEn = dto.NameEn;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.TaxNumber = dto.TaxNumber;
        customer.CreditLimit = dto.CreditLimit;
        customer.Notes = dto.Notes;
        customer.IsActive = dto.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(customer);
    }

    public async Task DeleteAsync(Guid id)
    {
        var customer = await _context.Set<Customer>()
            .Include(c => c.SalesInvoices)
            .Include(c => c.SalesReturns)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (customer is null)
            throw new InvalidOperationException("العميل غير موجود");

        if (customer.SalesInvoices.Any() || customer.SalesReturns.Any())
            throw new InvalidOperationException("لا يمكن حذف عميل له فواتير أو مردودات مسجلة.");

        customer.IsDeleted = true;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var customer = await _context.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (customer is null)
            throw new InvalidOperationException("العميل غير موجود");

        customer.IsActive = !customer.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}