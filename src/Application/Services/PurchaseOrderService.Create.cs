using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// PurchaseOrderService — الإنشاء والاعتماد والإلغاء.
/// </summary>
public partial class PurchaseOrderService
{
    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto)
    {
        // 1) التحقق من المورد
        var supplier = await _context.Set<Supplier>()
            .FirstOrDefaultAsync(s => s.Id == dto.SupplierId && !s.IsDeleted);
        if (supplier is null)
            throw new InvalidOperationException("المورد غير موجود");

        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new InvalidOperationException("يجب إضافة بند واحد على الأقل.");

        // 2) جلب الأصناف مرة واحدة والتحقق من وجودها وعدم تكرارها
        var distinctItemIds = dto.Lines.Select(l => l.ItemId).Distinct().ToList();
        if (distinctItemIds.Count != dto.Lines.Count)
            throw new InvalidOperationException("لا يمكن تكرار نفس الصنف في أمر واحد.");

        var items = await _context.Set<Item>()
            .Where(i => distinctItemIds.Contains(i.Id) && !i.IsDeleted)
            .ToListAsync();
        if (items.Count != distinctItemIds.Count)
            throw new InvalidOperationException("أحد الأصناف المحددة غير موجود.");

        // 3) بناء الأمر وحساب المجاميع (نفس نموذج الخصم والضريبة)
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = await NextOrderNumberAsync(),
            SupplierId = supplier.Id,
            OrderDate = dto.OrderDate,
            ExpectedDate = dto.ExpectedDate,
            Status = PurchaseOrderStatus.Draft,
            DiscountPercentage = dto.DiscountPercentage,
            TaxRate = dto.TaxRate,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        var subTotal = 0m;
        var lines = new List<PurchaseOrderLine>();
        foreach (var input in dto.Lines)
        {
            if (input.Quantity <= 0)
                throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");

            var line = new PurchaseOrderLine
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = order.Id,
                ItemId = input.ItemId,
                Quantity = input.Quantity,
                UnitPrice = input.UnitPrice,
                LineTotal = Math.Round(input.Quantity * input.UnitPrice, 2),
                CreatedAt = DateTime.UtcNow
            };
            lines.Add(line);
            subTotal += line.LineTotal;
        }

        order.SubTotal = Math.Round(subTotal, 2);
        order.DiscountAmount = Math.Round(order.SubTotal * (dto.DiscountPercentage / 100m), 2);
        var netBeforeTax = order.SubTotal - order.DiscountAmount;
        order.TaxAmount = Math.Round(netBeforeTax * (dto.TaxRate / 100m), 2);
        order.TotalAmount = netBeforeTax + order.TaxAmount;
        order.Lines = lines;

        _context.Set<PurchaseOrder>().Add(order);
        _context.Set<PurchaseOrderLine>().AddRange(lines);

        await _context.SaveChangesAsync();

        return MapToDto(order);
    }

    public async Task ApproveAsync(Guid id)
    {
        var order = await GetEntityAsync(id);
        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("لا يمكن اعتماد أمر غير مسودة.");
        order.Status = PurchaseOrderStatus.Approved;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(Guid id)
    {
        var order = await GetEntityAsync(id);
        if (order.Status == PurchaseOrderStatus.Converted)
            throw new InvalidOperationException("لا يمكن إلغاء أمر تم تحويله إلى فاتورة.");
        order.Status = PurchaseOrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}