using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements purchase order business logic (أوامر الشراء).
///
/// مثل عروض الأسعار: الأمر مستند "غير مُرحَّل" — كل عملياته (إنشاء/اعتماد/إلغاء)
/// لا تمسّ المخزون ولا الحسابات. الأثر الوحيد يحدث عند التحويل إلى فاتورة مشتريات،
/// حيث تستدعي خدمة الفواتير التي ترحّل "وارد مخزون" وتنشئ القيد المحاسبي.
/// </summary>
public partial class PurchaseOrderService : IPurchaseOrderService
{
    private readonly DbContext _context;
    private readonly IPurchaseInvoiceService _invoiceService;

    public PurchaseOrderService(DbContext context, IPurchaseInvoiceService invoiceService)
    {
        _context = context;
        _invoiceService = invoiceService;
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id)
    {
        var order = await _context.Set<PurchaseOrder>()
            .Include(o => o.Supplier)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        return order is null ? null : MapToDto(order);
    }

    public async Task<List<PurchaseOrderDto>> GetOrdersAsync()
    {
        var orders = await _context.Set<PurchaseOrder>()
            .Include(o => o.Supplier)
            .Include(o => o.Lines).ThenInclude(l => l.Item)
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }
}