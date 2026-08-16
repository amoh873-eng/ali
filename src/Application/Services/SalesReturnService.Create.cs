using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// SalesReturnService — إنشاء وترحيل المردود (CreateAsync).
/// </summary>
public partial class SalesReturnService
{
    public async Task<SalesReturnDto> CreateAsync(CreateSalesReturnDto dto)
    {
        // 1) الفاتورة الأصلية يجب أن تكون مرحّلة
        var invoice = await _context.Set<SalesInvoice>()
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == dto.SalesInvoiceId && !i.IsDeleted);
        if (invoice is null)
            throw new InvalidOperationException("الفاتورة الأصلية غير موجودة.");
        if (invoice.Status != DocumentStatus.Posted)
            throw new InvalidOperationException("لا يمكن عمل مردود لفاتورة غير مرحّلة.");

        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted);
        if (warehouse is null)
            throw new InvalidOperationException("المخزن غير موجود.");

        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new InvalidOperationException("يجب إضافة بند واحد على الأقل.");

        var distinctItemIds = dto.Lines.Select(l => l.ItemId).Distinct().ToList();
        if (distinctItemIds.Count != dto.Lines.Count)
            throw new InvalidOperationException("لا يمكن تكرار نفس الصنف في مردود واحد.");

        var invoiceLineByItem = invoice.Lines.ToDictionary(l => l.ItemId);

        // 2) بناء المردود واعتماد أسعار/تكاليف الفاتورة الأصلية لضمان التطابق
        var salesReturn = new SalesReturn
        {
            Id = Guid.NewGuid(),
            ReturnNumber = await NextReturnNumberAsync(),
            SalesInvoiceId = invoice.Id,
            CustomerId = invoice.CustomerId,
            WarehouseId = warehouse.Id,
            ReturnDate = dto.ReturnDate,
            Status = DocumentStatus.Posted,
            DiscountPercentage = invoice.DiscountPercentage,
            TaxRate = invoice.TaxRate,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        var subTotal = 0m;
        var lines = new List<SalesReturnLine>();
        var cogsTotal = 0m;

        foreach (var input in dto.Lines)
        {
            if (input.Quantity <= 0)
                throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");

            if (!invoiceLineByItem.TryGetValue(input.ItemId, out var invoiceLine))
                throw new InvalidOperationException("أحد الأصناف غير موجود في الفاتورة الأصلية.");

            // التحقق ألا يتجاوز المردود الكمية المباعة لكل صنف (بعد خصم ما سبق ردّه)
            var alreadyReturned = await AlreadyReturnedQuantityAsync(invoice.Id, input.ItemId);
            if (alreadyReturned + input.Quantity > invoiceLine.Quantity)
                throw new InvalidOperationException(
                    $"لا يمكن رد أكثر مما بيع. الصنف كمية مباعة {invoiceLine.Quantity:N0}، سبق ردّه {alreadyReturned:N0}.");

            // نستخدم سعر وتكلفة الفاتورة الأصلية حتى يعكس المردود البيع تماماً
            var line = new SalesReturnLine
            {
                Id = Guid.NewGuid(),
                SalesReturnId = salesReturn.Id,
                ItemId = input.ItemId,
                Quantity = input.Quantity,
                UnitPrice = invoiceLine.UnitPrice,
                UnitCost = invoiceLine.UnitCost,
                LineTotal = Math.Round(input.Quantity * invoiceLine.UnitPrice, 2),
                CreatedAt = DateTime.UtcNow
            };
            lines.Add(line);
            subTotal += line.LineTotal;
            cogsTotal += input.Quantity * invoiceLine.UnitCost;
        }

        salesReturn.SubTotal = Math.Round(subTotal, 2);
        salesReturn.DiscountAmount = Math.Round(salesReturn.SubTotal * (invoice.DiscountPercentage / 100m), 2);
        var netBeforeTax = salesReturn.SubTotal - salesReturn.DiscountAmount;
        salesReturn.TaxAmount = Math.Round(netBeforeTax * (invoice.TaxRate / 100m), 2);
        salesReturn.TotalAmount = Math.Round(netBeforeTax + salesReturn.TaxAmount, 2);
        salesReturn.Lines = lines;
        cogsTotal = Math.Round(cogsTotal, 2);

        _context.Set<SalesReturn>().Add(salesReturn);
        _context.Set<SalesReturnLine>().AddRange(lines);

        // 3) إعادة البضاعة إلى المخزن: حركة "وارد مردود" (موجبة) + تحديث رصيد الصنف
        var itemIds = lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await _context.Set<Item>()
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync();
        var itemDict = items.ToDictionary(i => i.Id);

        foreach (var line in lines)
        {
            _context.Set<StockMovement>().Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ItemId = line.ItemId,
                WarehouseId = warehouse.Id,
                MovementType = MovementType.SalesReturnIn,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                ReferenceNumber = salesReturn.ReturnNumber,
                Note = $"مردود فاتورة بيع {invoice.InvoiceNumber}",
                MovementDate = salesReturn.ReturnDate,
                CreatedAt = DateTime.UtcNow
            });

            itemDict[line.ItemId].CurrentStock += line.Quantity;
            itemDict[line.ItemId].UpdatedAt = DateTime.UtcNow;
        }

        // 4) القيد العكسي للبيع: مدين (مردودات + ضريبة) ، دائن (صندوق/عملاء)
        var returnsAccount = await GetAccountByCodeAsync(AccountSalesReturns);
        var taxPayableAccount = await GetAccountByCodeAsync(AccountSalesTaxPayable);
        var payableAccount = await GetAccountByCodeAsync(invoice.InvoiceType == SalesInvoiceType.OnAccount ? AccountReceivable : AccountCash);

        var reverseSaleEntry = await _journalService.PrepareEntryAsync(
            JournalEntryType.SalesReturn,
            salesReturn.ReturnDate,
            salesReturn.ReturnNumber,
            $"مردود فاتورة بيع {invoice.InvoiceNumber}",
            new List<JournalEntryLegInput>
            {
                new() { AccountId = returnsAccount.Id, DebitAmount = netBeforeTax },
                new() { AccountId = taxPayableAccount.Id, DebitAmount = salesReturn.TaxAmount },
                new() { AccountId = payableAccount.Id, CreditAmount = salesReturn.TotalAmount, Note = $"مردود {salesReturn.ReturnNumber}" }
            });

        // 5) القيد العكسي للتكلفة: مدين (مخزون) ، دائن (تكلفة البضاعة)
        var inventoryAccount = await GetAccountByCodeAsync(AccountInventory);
        var cogsAccount = await GetAccountByCodeAsync(AccountCostOfGoodsSold);

        var reverseCogsEntry = await _journalService.PrepareEntryAsync(
            JournalEntryType.SalesReturn,
            salesReturn.ReturnDate,
            salesReturn.ReturnNumber,
            $"عكس تكلفة مردود فاتورة بيع {invoice.InvoiceNumber}",
            new List<JournalEntryLegInput>
            {
                new() { AccountId = inventoryAccount.Id, DebitAmount = cogsTotal },
                new() { AccountId = cogsAccount.Id, CreditAmount = cogsTotal }
            });

        salesReturn.SalesJournalEntryId = reverseSaleEntry.Id;
        salesReturn.CogsJournalEntryId = reverseCogsEntry.Id;

        // 6) تقليل رصيد العميل بنفس مقدار المردود
        var customer = invoice.Customer;
        customer!.CurrentBalance -= salesReturn.TotalAmount;
        customer.UpdatedAt = DateTime.UtcNow;

        // SaveChanges واحدة: المردود + بنوده + حركاته + قيوده + الأرصدة ذرياً
        await _context.SaveChangesAsync();

        return MapToDto(salesReturn);
    }
}