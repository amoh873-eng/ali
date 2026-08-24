using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// Implements purchase invoice business logic (فواتير المشتريات) مع الربط المحاسبي:
/// - حركة مخزون "وارد مشتريات" لكل بند.
/// - قيد: مدين (مخزون البضاعة) ، دائن (الصندوق نقدي / الموردين آجل).
/// </summary>
public class PurchaseInvoiceService : IPurchaseInvoiceService
{
    private const string AccountInventory = "1300";
    private const string AccountCash = "1100";
    private const string AccountPayable = "2200";

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public PurchaseInvoiceService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    public async Task<List<PurchaseInvoiceDto>> GetInvoicesAsync()
    {
        var invoices = await _context.Set<PurchaseInvoice>()
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync();
        return invoices.Select(MapToDto).ToList();
    }

    public async Task<PurchaseInvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _context.Set<PurchaseInvoice>()
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        return invoice is null ? null : MapToDto(invoice);
    }

    public async Task<PurchaseInvoiceDto> CreateAsync(CreatePurchaseInvoiceDto dto)
    {
        var supplier = await _context.Set<Supplier>()
            .FirstOrDefaultAsync(s => s.Id == dto.SupplierId && !s.IsDeleted);
        if (supplier is null) throw new InvalidOperationException("المورد غير موجود");

        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted);
        if (warehouse is null) throw new InvalidOperationException("المخزن غير موجود");

        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new InvalidOperationException("يجب إضافة بند واحد على الأقل.");

        var type = (PurchaseInvoiceType)dto.InvoiceType;
        if (type != PurchaseInvoiceType.Cash && type != PurchaseInvoiceType.OnAccount)
            throw new InvalidOperationException("نوع الفاتورة غير صالح.");

        var distinctItemIds = dto.Lines.Select(l => l.ItemId).Distinct().ToList();
        if (distinctItemIds.Count != dto.Lines.Count)
            throw new InvalidOperationException("لا يمكن تكرار نفس الصنف في فاتورة واحدة.");

        var items = await _context.Set<Item>()
            .Where(i => distinctItemIds.Contains(i.Id) && !i.IsDeleted)
            .ToListAsync();
        if (items.Count != distinctItemIds.Count)
            throw new InvalidOperationException("أحد الأصناف المحددة غير موجود.");
        var itemDict = items.ToDictionary(i => i.Id);

        var invoice = new PurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = await NextInvoiceNumberAsync(),
            SupplierId = supplier.Id,
            WarehouseId = warehouse.Id,
            DueDate = dto.DueDate ?? dto.InvoiceDate.AddDays(30),
            TaxAmount = dto.Lines.Sum(l => l.Quantity * l.UnitCost) * 0m,
            InvoiceDate = dto.InvoiceDate,
            InvoiceType = type,
            Status = DocumentStatus.Posted,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        var subTotal = 0m;
        var lines = new List<PurchaseInvoiceLine>();

        foreach (var input in dto.Lines)
        {
            if (input.Quantity <= 0)
                throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");

            var line = new PurchaseInvoiceLine
            {
                Id = Guid.NewGuid(),
                PurchaseInvoiceId = invoice.Id,
                ItemId = input.ItemId,
                Quantity = input.Quantity,
                UnitCost = input.UnitCost,
                LineTotal = Math.Round(input.Quantity * input.UnitCost, 2),
                CreatedAt = DateTime.UtcNow
            };
            lines.Add(line);
            subTotal += line.LineTotal;
        }

        invoice.SubTotal = Math.Round(subTotal, 2);
        invoice.TotalAmount = invoice.SubTotal;
        invoice.PaidAmount = type == PurchaseInvoiceType.Cash ? invoice.TotalAmount : 0m;
        invoice.Lines = lines;

        _context.Set<PurchaseInvoice>().Add(invoice);
        _context.Set<PurchaseInvoiceLine>().AddRange(lines);

        foreach (var line in lines)
        {
            _context.Set<StockMovement>().Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ItemId = line.ItemId,
                WarehouseId = warehouse.Id,
                MovementType = MovementType.PurchaseReceipt,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                ReferenceNumber = invoice.InvoiceNumber,
                Note = $"فاتورة مشتريات {invoice.InvoiceNumber}",
                MovementDate = invoice.InvoiceDate,
                CreatedAt = DateTime.UtcNow
            });

            // تقييم المتوسط المرجح: عند كل وارد نعيد حساب تكلفة الوحدة
            // newCost = (الرصيد القديم × التكلفة القديمة + الوارد × تكلفته) / إجمالي الكمية
            var item = itemDict[line.ItemId];
            item.CostPrice = InventoryValuation.ComputeWeightedAverage(
                item.CurrentStock, item.CostPrice, line.Quantity, line.UnitCost);
            item.CurrentStock += line.Quantity;
            item.UpdatedAt = DateTime.UtcNow;
        }

        var inventoryAccount = await GetAccountByCodeAsync(AccountInventory);
        var creditAccount = await GetAccountByCodeAsync(
            type == PurchaseInvoiceType.Cash ? AccountCash : AccountPayable);

        var entry = await _journalService.PrepareEntryAsync(
            JournalEntryType.PurchaseInvoice,
            invoice.InvoiceDate,
            invoice.InvoiceNumber,
            $"فاتورة مشتريات {invoice.InvoiceNumber} - {supplier.NameAr}",
            new List<JournalEntryLegInput>
            {
                new() { AccountId = inventoryAccount.Id, DebitAmount = invoice.TotalAmount, Note = $"فاتورة {invoice.InvoiceNumber}" },
                new() { AccountId = creditAccount.Id, CreditAmount = invoice.TotalAmount }
            });

        invoice.JournalEntryId = entry.Id;

        supplier.CurrentBalance += invoice.TotalAmount;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(invoice);
    }

    private Task<string> NextInvoiceNumberAsync()
        => NumberSequenceHelper.NextAsync(_context, "PI");

    private async Task<Account> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي '{code}' غير موجود في شجرة الحسابات.");
        return account;
    }

    private static PurchaseInvoiceDto MapToDto(PurchaseInvoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        SupplierId = invoice.SupplierId,
        SupplierCode = invoice.Supplier?.Code ?? "—",
        SupplierName = invoice.Supplier?.NameAr ?? "—",
        WarehouseId = invoice.WarehouseId,
        WarehouseName = invoice.Warehouse?.NameAr ?? "—",
        InvoiceDate = invoice.InvoiceDate,
        InvoiceType = (int)invoice.InvoiceType,
        Status = (int)invoice.Status,
        SubTotal = invoice.SubTotal,
        TotalAmount = invoice.TotalAmount,
        PaidAmount = invoice.PaidAmount,
        Note = invoice.Note,
        Lines = invoice.Lines.Select(l => new PurchaseInvoiceLineDto
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