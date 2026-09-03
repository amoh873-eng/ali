using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// SalesInvoiceService — الأدوات المساعدة (الترقيم، التحقق من الرصيد، اعتماد الحسابات، التحويل).
/// </summary>
public partial class SalesInvoiceService
{
    private Task<string> NextInvoiceNumberAsync()
        => NumberSequenceHelper.NextAsync(_context, "SI");

    /// <summary>
    /// يبحث عن حساب نظامي في شجرة الحسابات حسب كوده الثابت.
    /// هذه الحسابات مُزروعة مسبقاً في SeedSalesAccounts.
    /// </summary>
    private async Task<Account> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي '{code}' غير موجود في شجرة الحسابات.");
        return account;
    }

    private static SalesInvoiceDto MapToDto(SalesInvoice invoice)
    {
        return new SalesInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            CustomerCode = invoice.Customer?.Code ?? "—",
            CustomerName = invoice.Customer?.NameAr ?? "—",
            WarehouseId = invoice.WarehouseId,
            WarehouseName = invoice.Warehouse?.NameAr ?? "—",
            InvoiceDate = invoice.InvoiceDate,
            InvoiceType = (int)invoice.InvoiceType,
            PaymentMethod = (int)invoice.PaymentMethod,
            CardApprovalCode = invoice.CardApprovalCode,
            CardLast4 = invoice.CardLast4,
            CardNetwork = invoice.CardNetwork,
            CardTransactionAt = invoice.CardTransactionAt,
            Status = (int)invoice.Status,
            SubTotal = invoice.SubTotal,
            DiscountPercentage = invoice.DiscountPercentage,
            DiscountAmount = invoice.DiscountAmount,
            TaxRate = invoice.TaxRate,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            Note = invoice.Note,
            SalesJournalEntryId = invoice.SalesJournalEntryId,
            CogsJournalEntryId = invoice.CogsJournalEntryId,
            IsPos = invoice.IsPos,
            JoFotaraReferenceNumber = invoice.JoFotaraReferenceNumber,
            JoFotaraQrCode = invoice.JoFotaraQrCode,
            JoFotaraSubmittedAt = invoice.JoFotaraSubmittedAt,
            JoFotaraStatus = (int)invoice.JoFotaraStatus,
            Lines = invoice.Lines.Select(MapLineToDto).ToList()
        };
    }

    private static SalesInvoiceLineDto MapLineToDto(SalesInvoiceLine line)
    {
        return new SalesInvoiceLineDto
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