using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// SalesReturnService — الأدوات المساعدة (الترقيم، حساب ما سبق ردّه، اعتماد الحسابات، التحويل).
/// </summary>
public partial class SalesReturnService
{
    private Task<string> NextReturnNumberAsync()
        => NumberSequenceHelper.NextAsync(_context, "SR");

    /// <summary>
    /// يحسب الكمية التي سبق ردّها لصنف معين في فاتورة معينة
    /// (مجموع الكميات في كل المردودات المرحّلة المرتبطة بتلك الفاتورة لذاك الصنف).
    /// </summary>
    private async Task<decimal> AlreadyReturnedQuantityAsync(Guid invoiceId, Guid itemId)
    {
        var query =
            from r in _context.Set<SalesReturn>()
            from rl in r.Lines
            where r.SalesInvoiceId == invoiceId
                  && r.Status == DocumentStatus.Posted
                  && rl.ItemId == itemId
            select rl.Quantity;

        return await query.SumAsync();
    }

    /// <summary>
    /// الكمية المتبقية القابلة للرد لكل صنف في فاتورة (الكمية المباعة − ما سبق ردّه).
    /// يعيد قاموساً: ItemId → الكمية المتبقية (صفر إن لم يبقَ شيء أو لم يوجد الصنف).
    /// </summary>
    public async Task<Dictionary<Guid, decimal>> GetRemainingReturnableAsync(Guid invoiceId)
    {
        var invoice = await _context.Set<SalesInvoice>()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted);

        if (invoice is null)
            return new Dictionary<Guid, decimal>();

        var result = new Dictionary<Guid, decimal>();
        foreach (var line in invoice.Lines)
        {
            var already = await AlreadyReturnedQuantityAsync(invoiceId, line.ItemId);
            result[line.ItemId] = Math.Max(0m, line.Quantity - already);
        }

        return result;
    }

    /// <summary>
    /// يبحث عن حساب نظامي حسب كوده الثابت (يجب مطابقة أكواد SeedSalesAccounts).
    /// </summary>
    private async Task<Account> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي '{code}' غير موجود في شجرة الحسابات.");
        return account;
    }

    private static SalesReturnDto MapToDto(SalesReturn salesReturn)
    {
        return new SalesReturnDto
        {
            Id = salesReturn.Id,
            ReturnNumber = salesReturn.ReturnNumber,
            SalesInvoiceId = salesReturn.SalesInvoiceId,
            InvoiceNumber = salesReturn.SalesInvoice?.InvoiceNumber ?? "—",
            CustomerId = salesReturn.CustomerId,
            CustomerName = salesReturn.Customer?.NameAr ?? "—",
            WarehouseId = salesReturn.WarehouseId,
            WarehouseName = salesReturn.Warehouse?.NameAr ?? "—",
            ReturnDate = salesReturn.ReturnDate,
            Status = (int)salesReturn.Status,
            SubTotal = salesReturn.SubTotal,
            DiscountPercentage = salesReturn.DiscountPercentage,
            DiscountAmount = salesReturn.DiscountAmount,
            TaxRate = salesReturn.TaxRate,
            TaxAmount = salesReturn.TaxAmount,
            TotalAmount = salesReturn.TotalAmount,
            Note = salesReturn.Note,
            Lines = salesReturn.Lines.Select(MapLineToDto).ToList()
        };
    }

    private static SalesReturnLineDto MapLineToDto(SalesReturnLine line)
    {
        return new SalesReturnLineDto
        {
            Id = line.Id,
            ItemId = line.ItemId,
            ItemCode = line.Item?.Code ?? "—",
            ItemNameAr = line.Item?.NameAr ?? "—",
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            UnitCost = line.UnitCost,
            LineTotal = line.LineTotal
        };
    }
}