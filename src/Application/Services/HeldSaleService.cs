using ERPSystem.Application.DTOs.Pos;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// تطبيق خدمة عمليات البيع الموقوفة. تخزّن لقطة JSON فقط — لا تُنشئ أي
/// StockMovement أو JournalEntry؛ تتحوّل إلى فاتورة عند اكتمالها لاحقاً.
/// </summary>
public class HeldSaleService : IHeldSaleService
{
    private readonly DbContext _context;

    public HeldSaleService(DbContext context)
    {
        _context = context;
    }

    public async Task<HeldSaleDto> HoldAsync(SaveHeldSaleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CashierUserId))
            throw new InvalidOperationException("مطلوب معرّف أمين الصندوق لحفظ عملية موقوفة.");
        // نموذج مجاني مبسط: يُسمح بحفظ سلة فارغة؟ لا — يجب أن تحتوي على بند.
        if (string.IsNullOrWhiteSpace(dto.LinesJson))
            throw new InvalidOperationException("لا يمكن إيقاف سلة فارغة.");

        var entity = new HeldSale
        {
            Id = Guid.NewGuid(),
            CashierUserId = dto.CashierUserId,
            CustomerId = dto.CustomerId,
            LinesJson = dto.LinesJson,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };
        _context.Set<HeldSale>().Add(entity);
        await _context.SaveChangesAsync();

        return MapToDto(entity, null);
    }

    public async Task<List<HeldSaleDto>> GetByCashierAsync(string cashierUserId)
    {
        var items = await _context.Set<HeldSale>()
            .Where(h => h.CashierUserId == cashierUserId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        var customerIds = items.Where(i => i.CustomerId.HasValue).Select(i => i.CustomerId!.Value).Distinct().ToList();
        var customers = customerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Set<Customer>()
                .Where(c => customerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.NameAr);

        return items.Select(i => MapToDto(i, i.CustomerId.HasValue && customers.TryGetValue(i.CustomerId.Value, out var n) ? n : null)).ToList();
    }

    public async Task<HeldSaleDto?> GetByIdAsync(Guid id)
    {
        var e = await _context.Set<HeldSale>().FirstOrDefaultAsync(h => h.Id == id);
        if (e is null) return null;
        var customerName = e.CustomerId.HasValue
            ? await _context.Set<Customer>().Where(c => c.Id == e.CustomerId.Value).Select(c => c.NameAr).FirstOrDefaultAsync()
            : null;
        return MapToDto(e, customerName);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var e = await _context.Set<HeldSale>().FirstOrDefaultAsync(h => h.Id == id);
        if (e is null) return false;
        _context.Set<HeldSale>().Remove(e);
        await _context.SaveChangesAsync();
        return true;
    }

    private static HeldSaleDto MapToDto(HeldSale e, string? customerName)
        => new()
        {
            Id = e.Id,
            CashierUserId = e.CashierUserId,
            CreatedAt = e.CreatedAt,
            CustomerId = e.CustomerId,
            CustomerName = customerName ?? "زبون نقدي",
            LinesJson = e.LinesJson,
            Note = e.Note,
            IsStale = DateTime.UtcNow - e.CreatedAt > TimeSpan.FromHours(24)
        };
}