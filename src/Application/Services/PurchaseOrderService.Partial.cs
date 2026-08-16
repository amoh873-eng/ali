using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// PurchaseOrderService — التحويل إلى فاتورة مشتريات والأدوات المساعدة.
/// </summary>
public partial class PurchaseOrderService
{
    public async Task<PurchaseOrderDto> ConvertToInvoiceAsync(Guid id, Guid warehouseId, int invoiceType)
    {
        var order = await GetEntityAsync(id);
        if (order.Status == PurchaseOrderStatus.Converted)
            throw new InvalidOperationException("هذا الأمر محوّل إلى فاتورة بالفعل.");
        if (order.Status == PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException("لا يمكن تحويل أمر ملغي.");

        // نستدعي خدمة فواتير المشتريات (ترحّل "وارد مخزون" + قيداً محاسبياً)
        // ليبقى منطق الترحيل في مكان واحد فقط.
        var invoiceDto = await _invoiceService.CreateAsync(new CreatePurchaseInvoiceDto
        {
            SupplierId = order.SupplierId,
            WarehouseId = warehouseId,
            InvoiceDate = DateTime.Today,
            InvoiceType = invoiceType,
            Note = $"محوّل من أمر شراء {order.OrderNumber}",
            Lines = order.Lines.Select(l => new CreatePurchaseInvoiceLineDto
            {
                ItemId = l.ItemId,
                Quantity = l.Quantity,
                UnitCost = l.UnitPrice
            }).ToList()
        });

        order.ConvertedInvoiceId = invoiceDto.Id;
        order.Status = PurchaseOrderStatus.Converted;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(order);
    }

    private async Task<PurchaseOrder> GetEntityAsync(Guid id)
    {
        var order = await _context.Set<PurchaseOrder>()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
        if (order is null)
            throw new InvalidOperationException("الأمر غير موجود.");
        return order;
    }

    private Task<string> NextOrderNumberAsync()
        => NumberSequenceHelper.NextAsync(_context, "PO");

    private static PurchaseOrderDto MapToDto(PurchaseOrder order)
    {
        return new PurchaseOrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            SupplierId = order.SupplierId,
            SupplierCode = order.Supplier?.Code ?? "—",
            SupplierName = order.Supplier?.NameAr ?? "—",
            OrderDate = order.OrderDate,
            ExpectedDate = order.ExpectedDate,
            Status = (int)order.Status,
            SubTotal = order.SubTotal,
            DiscountPercentage = order.DiscountPercentage,
            DiscountAmount = order.DiscountAmount,
            TaxRate = order.TaxRate,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            Note = order.Note,
            ConvertedInvoiceId = order.ConvertedInvoiceId,
            Lines = order.Lines.Select(MapLineToDto).ToList()
        };
    }

    private static PurchaseOrderLineDto MapLineToDto(PurchaseOrderLine line)
    {
        return new PurchaseOrderLineDto
        {
            Id = line.Id,
            ItemId = line.ItemId,
            ItemCode = line.Item?.Code ?? "—",
            ItemNameAr = line.Item?.NameAr ?? "—",
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineTotal = line.LineTotal
        };
    }
}