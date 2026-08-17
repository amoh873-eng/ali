using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements purchase return business logic (مردودات المشتريات): يعيد البضاعة للمورد،
/// ويسجّل حركة مخزون "صادر مردود مشتريات" ويعكس القيد المحاسبي.
/// </summary>
public class PurchaseReturnService : IPurchaseReturnService
{
    private const string AccountInventory = "1300";
    private const string AccountCash = "1100";
    private const string AccountPayable = "2200";

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public PurchaseReturnService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    public async Task<List<PurchaseReturnDto>> GetReturnsAsync()
    {
        var returns = await _context.Set<PurchaseReturn>()
            .Include(r => r.Supplier)
            .Include(r => r.Warehouse)
            .Include(r => r.PurchaseInvoice)
            .OrderByDescending(r => r.ReturnDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
        return returns.Select(MapToDto).ToList();
    }

    public async Task<PurchaseReturnDto?> GetByIdAsync(Guid id)
    {
        var purchaseReturn = await _context.Set<PurchaseReturn>()
            .Include(r => r.Supplier)
            .Include(r => r.Warehouse)
            .Include(r => r.PurchaseInvoice)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        return purchaseReturn is null ? null : MapToDto(purchaseReturn);
    }

    public async Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnDto dto)
    {
        var invoice = await _context.Set<PurchaseInvoice>()
            .Include(i => i.Lines)
            .Include(i => i.Supplier)
            .FirstOrDefaultAsync(i => i.Id == dto.PurchaseInvoiceId && !i.IsDeleted);
        if (invoice is null) throw new InvalidOperationException("الفاتورة الأصلية غير موجودة.");
        if (invoice.Status != DocumentStatus.Posted)
            throw new InvalidOperationException("لا يمكن عمل مردود لفاتورة غير مرحّلة.");

        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted);
        if (warehouse is null) throw new InvalidOperationException("المخزن غير موجود.");

        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new InvalidOperationException("يجب إضافة بند واحد على الأقل.");

        var invoiceLineByItem = invoice.Lines.ToDictionary(l => l.ItemId);

        var purchaseReturn = new PurchaseReturn
        {
            Id = Guid.NewGuid(),
            ReturnNumber = await NextReturnNumberAsync(),
            PurchaseInvoiceId = invoice.Id,
            SupplierId = invoice.SupplierId,
            WarehouseId = warehouse.Id,
            ReturnDate = dto.ReturnDate,
            Status = DocumentStatus.Posted,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        var subTotal = 0m;
        var lines = new List<PurchaseReturnLine>();

        foreach (var input in dto.Lines)
        {
            if (input.Quantity <= 0)
                throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");

            if (!invoiceLineByItem.TryGetValue(input.ItemId, out var invoiceLine))
                throw new InvalidOperationException("أحد الأصناف غير موجود في الفاتورة الأصلية.");

            var alreadyReturned = await AlreadyReturnedQuantityAsync(invoice.Id, input.ItemId);
            if (alreadyReturned + input.Quantity > invoiceLine.Quantity)
                throw new InvalidOperationException($"لا يمكن رد أكثر مما اشتُري. الكمية المشتراة {invoiceLine.Quantity:N0}.");

            var line = new PurchaseReturnLine
            {
                Id = Guid.NewGuid(),
                PurchaseReturnId = purchaseReturn.Id,
                ItemId = input.ItemId,
                Quantity = input.Quantity,
                UnitCost = invoiceLine.UnitCost,
                LineTotal = Math.Round(input.Quantity * invoiceLine.UnitCost, 2),
                CreatedAt = DateTime.UtcNow
            };
            lines.Add(line);
            subTotal += line.LineTotal;
        }

        purchaseReturn.SubTotal = Math.Round(subTotal, 2);
        purchaseReturn.TotalAmount = purchaseReturn.SubTotal;
        purchaseReturn.Lines = lines;

        _context.Set<PurchaseReturn>().Add(purchaseReturn);
        _context.Set<PurchaseReturnLine>().AddRange(lines);

        var itemIds = lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await _context.Set<Item>().Where(i => itemIds.Contains(i.Id)).ToListAsync();
        var itemDict = items.ToDictionary(i => i.Id);

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var line in lines)
            {
                // تحقق من كفاية رصيد الصنف في المخزن قبل إخراج البضاعة (مردود للمورد)
                await StockAvailabilityHelper.EnsureEnoughStockAsync(_context, line.ItemId, warehouse.Id, line.Quantity);

                _context.Set<StockMovement>().Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ItemId = line.ItemId,
                WarehouseId = warehouse.Id,
                MovementType = MovementType.PurchaseReturnOut,
                Quantity = -line.Quantity,
                UnitCost = line.UnitCost,
                ReferenceNumber = purchaseReturn.ReturnNumber,
                Note = $"مردود فاتورة مشتريات {invoice.InvoiceNumber}",
                MovementDate = purchaseReturn.ReturnDate,
                CreatedAt = DateTime.UtcNow
            });

            itemDict[line.ItemId].CurrentStock -= line.Quantity;
            itemDict[line.ItemId].UpdatedAt = DateTime.UtcNow;
        }

        var inventoryAccount = await GetAccountByCodeAsync(AccountInventory);
        var debitAccount = await GetAccountByCodeAsync(
            invoice.InvoiceType == PurchaseInvoiceType.OnAccount ? AccountPayable : AccountCash);

        var entry = await _journalService.PrepareEntryAsync(
            JournalEntryType.PurchaseReturn,
            purchaseReturn.ReturnDate,
            purchaseReturn.ReturnNumber,
            $"مردود فاتورة مشتريات {invoice.InvoiceNumber}",
            new List<JournalEntryLegInput>
            {
                new() { AccountId = debitAccount.Id, DebitAmount = purchaseReturn.TotalAmount, Note = $"مردود {purchaseReturn.ReturnNumber}" },
                new() { AccountId = inventoryAccount.Id, CreditAmount = purchaseReturn.TotalAmount }
            });

        purchaseReturn.JournalEntryId = entry.Id;

        var supplier = invoice.Supplier;
        supplier!.CurrentBalance -= purchaseReturn.TotalAmount;
        supplier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return MapToDto(purchaseReturn);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException("تعارض في تحديث بيانات المخزون — حاول مرة أخرى.");
        }
    }

    private Task<string> NextReturnNumberAsync()
        => NumberSequenceHelper.NextAsync(_context, "PR");

    private async Task<decimal> AlreadyReturnedQuantityAsync(Guid invoiceId, Guid itemId)
    {
        var query =
            from r in _context.Set<PurchaseReturn>()
            from rl in r.Lines
            where r.PurchaseInvoiceId == invoiceId
                  && r.Status == DocumentStatus.Posted
                  && rl.ItemId == itemId
            select rl.Quantity;
        return await query.SumAsync();
    }

    private async Task<Account> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي '{code}' غير موجود في شجرة الحسابات.");
        return account;
    }

    private static PurchaseReturnDto MapToDto(PurchaseReturn r) => new()
    {
        Id = r.Id,
        ReturnNumber = r.ReturnNumber,
        PurchaseInvoiceId = r.PurchaseInvoiceId,
        InvoiceNumber = r.PurchaseInvoice?.InvoiceNumber ?? "—",
        SupplierId = r.SupplierId,
        SupplierName = r.Supplier?.NameAr ?? "—",
        WarehouseId = r.WarehouseId,
        WarehouseName = r.Warehouse?.NameAr ?? "—",
        ReturnDate = r.ReturnDate,
        Status = (int)r.Status,
        SubTotal = r.SubTotal,
        TotalAmount = r.TotalAmount,
        Note = r.Note,
        Lines = r.Lines.Select(l => new PurchaseReturnLineDto
        {
            Id = l.Id,
            ItemId = l.ItemId,
            ItemCode = l.Item?.Code ?? "—",
            ItemNameAr = l.Item?.NameAr ?? "—",
            Quantity = l.Quantity,
            UnitCost = l.UnitCost,
            LineTotal = l.LineTotal
        }).ToList()
    };
}