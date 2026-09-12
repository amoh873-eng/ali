using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// سندات الإتلاف (Stock Write-Off) — خصم المخزون + قيد محاسبي متوازن في معاملة واحدة ذرية.
///
/// نفس أسلوب الترحيل المحاسبي التلقائي في SalesInvoiceService/PurchaseInvoiceService:
///   - IJournalEntryService.PrepareEntryAsync يُجهّز القيد في السياق دون حفظ فوري (يتحقق التوازن).
///   - NumberSequenceHelper للترقيم (بادئة WO).
///   - StockAvailabilityHelper للتحقق من كفاية الرصيد مع قفل صف الصنف.
///   - SaveChangesAsync واحدة داخل معاملة → إما كل شيء (المخزون + الدُفعة + الحركة + السند + القيد) أو لا شيء.
///
/// القيد: مدين "مصروف الهالك/التوالف" (يُنشأ تلقائياً إن غاب، ضمن مجموعة المصروفات)
///       دائن "المخزون" (1300) — نفس الحساب في قيود المبيعات/المشتريات.
/// المبلغ = Quantity × UnitCost (سعر التكلفة يُنسَخ وقت التسجيل ولا يُعاد حسابه لاحقاً).
/// </summary>
public class StockWriteOffService : IStockWriteOffService
{
    // نفس حساب المخزون المستخدم في قيود المبيعات/المشتريات الحالية
    private const string AccountInventory = "1300";

    // حساب "مصروف الهالك/التوالف" — يُبحث عنه أولاً بالكود، ويُنشأ تلقائياً عند غيابه
    // كحساب مصروف (طبيعته مدين) تحت جذر المصروفات (كود 5).
    private const string AccountWasteExpense = "5300";

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public StockWriteOffService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    public async Task<StockWriteOffDto> CreateAsync(CreateStockWriteOffDto dto, string createdByUserId)
    {
        if (dto.Quantity <= 0m)
            throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");

        var item = await _context.Set<Item>()
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId && !i.IsDeleted);
        if (item is null)
            throw new InvalidOperationException("الصنف غير موجود");

        var warehouse = await _context.Set<Warehouse>()
            .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted);
        if (warehouse is null)
            throw new InvalidOperationException("المخزن غير موجود");

        if (!Enum.IsDefined(typeof(StockWriteOffReason), dto.Reason))
            throw new InvalidOperationException("سبب الإتلاف غير صالح.");
        var reason = (StockWriteOffReason)dto.Reason;

        if (reason == StockWriteOffReason.Other && string.IsNullOrWhiteSpace(dto.OtherReasonText))
            throw new InvalidOperationException("عند اختيار سبب (أخرى) يجب توضيح السبب في الحقل النصي.");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // قفل صف الصنف + فحص كفاية الرصيد (نفس الانضباط المُتّبع في بقية الخدمات)
            await StockAvailabilityHelper.EnsureEnoughStockAsync(_context, item.Id, warehouse.Id, dto.Quantity);

            // سعر التكلفة يُنسَخ وقت التسجيل (نسخة ثابتة) ولا يُعاد حسابه لاحقاً لو تغيّر السعر مستقبلاً
            var unitCost = item.CostPrice;
            var total = Math.Round(dto.Quantity * unitCost, 2);

            // ── خصم من دُفعة محددة للأصناف التي تتتبّع دُفعات (Item.TracksBatches) ──
            // يشتغل داخل نفس المعاملة/القفل الحالي ويُسمح بإتلاف دُفعة منتهية الصلاحية (غالباً هدف السند).
            ItemBatch? batch = null;
            if (item.TracksBatches)
            {
                if (dto.BatchId is null || dto.BatchId == Guid.Empty)
                    throw new InvalidOperationException("هذا الصنف يتتبّع الدُفعات — اختر الدُفعة المراد إتلافها.");

                batch = await _context.Set<ItemBatch>()
                    .FirstOrDefaultAsync(b => b.Id == dto.BatchId.Value
                                              && b.ItemId == item.Id
                                              && b.WarehouseId == warehouse.Id);
                if (batch is null)
                    throw new InvalidOperationException("الدُفعة المحددة غير موجودة لهذا الصنف في المخزن المحدد.");

                if (batch.Quantity < dto.Quantity)
                    throw new InvalidOperationException(
                        $"رصيد الدُفعة المحددة غير كافٍ. المتاح: {batch.Quantity:N0}، المطلوب: {dto.Quantity:N0}.");

                batch.Quantity -= dto.Quantity;

                // ضمانة التكامل (نفس المعادلة الحاكمة): مجموع الدُفعات == مجموع حركات المخزون
                await AssertBatchesConsistentAsync(item.Id, warehouse.Id);
            }

            var documentNumber = await NumberSequenceHelper.NextAsync(_context, "WO");

            // حركة مخزون صادرة (تُسجَّل سالبة كمثيلاتها من الحركات الصادرة — تعكس الخصم في سجل الحركات)
            _context.Set<StockMovement>().Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                WarehouseId = warehouse.Id,
                MovementType = MovementType.WriteOff,
                Quantity = -dto.Quantity,
                UnitCost = unitCost,
                ReferenceNumber = documentNumber,
                Note = $"سند إتلاف {documentNumber}",
                MovementDate = dto.Date,
                CreatedAt = DateTime.UtcNow
            });

            // تحديث الرصيد الكلي المكرر للصنف (للأداء في لوحة التحكم)
            item.CurrentStock -= dto.Quantity;
            item.UpdatedAt = DateTime.UtcNow;

            // ── القيد المحاسبي المتوازن: مدين مصروف الهالك / دائن المخزون ──
            // الحساب الجديد (5300) يُثبَّت داخل نفس المعاملة قبل تجهيز القيد لأن
            // PrepareEntryAsync يقرأ الحسابات من قاعدة البيانات (لا من سياق غير محفوظ).
            var wasteAccount = await GetOrCreateWasteExpenseAccountAsync();
            await _context.SaveChangesAsync();
            var inventoryAccount = await GetAccountByCodeAsync(AccountInventory);

            var entry = await _journalService.PrepareEntryAsync(
                JournalEntryType.StockWriteOff,
                dto.Date,
                documentNumber,
                $"سند إتلاف {documentNumber} - {item.NameAr} ({GetReasonNameAr(reason)})",
                new List<JournalEntryLegInput>
                {
                    new() { AccountId = wasteAccount.Id, DebitAmount = total, Note = $"إتلاف {dto.Quantity:N0} × {unitCost:N2}" },
                    new() { AccountId = inventoryAccount.Id, CreditAmount = total }
                });

            var writeOff = new StockWriteOff
            {
                Id = Guid.NewGuid(),
                DocumentNumber = documentNumber,
                ItemId = item.Id,
                WarehouseId = warehouse.Id,
                BatchId = item.TracksBatches ? dto.BatchId : null,
                Quantity = dto.Quantity,
                Reason = reason,
                OtherReasonText = reason == StockWriteOffReason.Other ? dto.OtherReasonText?.Trim() : null,
                Date = dto.Date,
                UnitCost = unitCost,
                Notes = dto.Notes,
                CreatedByUserId = createdByUserId,
                JournalEntryId = entry.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdByUserId
            };
            _context.Set<StockWriteOff>().Add(writeOff);

            // SaveChanges واحدة → كل شيء أو لا شيء (السند + الدُفعة + الحركة + الرصيد + القيد + الحساب الجديد)
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            var saved = await LoadDtoAsync(writeOff.Id);
            return saved ?? throw new InvalidOperationException("فشل قراءة السند بعد الحفظ — راجع البيانات.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException("تعارض في تحديث بيانات السند — حاول مرة أخرى.");
        }
    }

    public async Task<List<StockWriteOffDto>> GetAsync(Guid? itemId, int? reason, DateTime? from, DateTime? to)
    {
        var query = _context.Set<StockWriteOff>().AsNoTracking()
            .Include(w => w.Item)
            .Include(w => w.Warehouse)
            .Include(w => w.Batch)
            .Include(w => w.JournalEntry)
            .AsQueryable();

        if (itemId.HasValue) query = query.Where(w => w.ItemId == itemId.Value);
        if (reason.HasValue) query = query.Where(w => (int)w.Reason == reason.Value);
        if (from.HasValue) query = query.Where(w => w.Date >= from.Value);
        if (to.HasValue)
        {
            var toEnd = to.Value.Date.AddDays(1);
            query = query.Where(w => w.Date < toEnd);
        }

        var rows = await query
            .OrderByDescending(w => w.Date)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync();

        return rows.Select(MapToDto).ToList();
    }

    public async Task<StockWriteOffDto?> GetByIdAsync(Guid id)
        => await LoadDtoAsync(id);

    public async Task<List<ItemBatchOptionDto>> GetBatchesForAsync(Guid itemId, Guid warehouseId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var batches = await _context.Set<ItemBatch>()
            .Where(b => b.ItemId == itemId && b.WarehouseId == warehouseId && b.Quantity > 0m)
            .OrderBy(b => b.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(b => b.ExpiryDate)
            .ThenByDescending(b => b.ReceivedDate)
            .ToListAsync();

        return batches.Select(b => new ItemBatchOptionDto
        {
            Id = b.Id,
            ItemId = b.ItemId,
            WarehouseId = b.WarehouseId,
            BatchNumber = b.BatchNumber,
            ExpiryDate = b.ExpiryDate,
            Quantity = b.Quantity,
            Status = ResolveStatus(b.ExpiryDate, today)
        }).ToList();
    }

    public async Task<StockWriteOffSummaryDto> GetSummaryAsync(Guid? itemId, int? reason, DateTime? from, DateTime? to)
    {
        var query = _context.Set<StockWriteOff>().AsNoTracking();

        if (itemId.HasValue) query = query.Where(w => w.ItemId == itemId.Value);
        if (reason.HasValue) query = query.Where(w => (int)w.Reason == reason.Value);
        if (from.HasValue) query = query.Where(w => w.Date >= from.Value);
        if (to.HasValue)
        {
            var toEnd = to.Value.Date.AddDays(1);
            query = query.Where(w => w.Date < toEnd);
        }

        // التجميع حسب السبب (يُنفَّذ في قاعدة البيانات)
        var byReason = await query
            .GroupBy(w => w.Reason)
            .Select(g => new StockWriteOffReasonSummaryDto
            {
                Reason = (int)g.Key,
                Count = g.Count(),
                Quantity = g.Sum(w => w.Quantity),
                Value = Math.Round(g.Sum(w => w.Quantity * w.UnitCost), 2)
            })
            .OrderByDescending(r => r.Value)
            .ToListAsync();

        foreach (var row in byReason)
            row.ReasonNameAr = GetReasonNameAr((StockWriteOffReason)row.Reason);

        // التوزيع الشهري: نجلب خفيفاً ثم نجمّع في الذاكرة (ترقيم السنة/الشهر)
        var rows = await query
            .Select(w => new { w.Date, Value = w.Quantity * w.UnitCost })
            .ToListAsync();

        var byMonth = rows
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => new StockWriteOffMonthlyDto
            {
                Month = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                MonthLabel = GetMonthNameAr(g.Key.Month, g.Key.Year),
                Value = Math.Round(g.Sum(x => x.Value), 2)
            })
            .ToList();

        return new StockWriteOffSummaryDto
        {
            Count = rows.Count,
            TotalQuantity = byReason.Sum(r => r.Quantity),
            TotalValue = Math.Round(rows.Sum(x => x.Value), 2),
            ByReason = byReason,
            ByMonth = byMonth
        };
    }

    // ==================== Helpers ====================

    private async Task<StockWriteOffDto?> LoadDtoAsync(Guid id)
    {
        var row = await _context.Set<StockWriteOff>()
            .Include(w => w.Item)
            .Include(w => w.Warehouse)
            .Include(w => w.Batch)
            .Include(w => w.JournalEntry)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

        return row is null ? null : MapToDto(row);
    }

    /// <summary>
    /// يبحث عن حساب "مصروف الهالك/التوالف" بالكود الثابت؛ إن غاب يُنشئه تلقائياً
    /// كحساب مصروف (طبيعته مدين) تحت جذر المصروفات (كود 5) وهو داخل نفس المعاملة —
    /// فلا يبقى حساب يتيم لو فشل ترحيل السند. الاسمان المعتمدان:
    /// العربية "مصروف الهالك/التوالف" / الإنجليزية "Waste and Spoilage Expense".
    /// </summary>
    private async Task<Account> GetOrCreateWasteExpenseAccountAsync()
    {
        var existing = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == AccountWasteExpense && !a.IsDeleted);
        if (existing is not null)
            return existing;

        var expenseRoot = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == "5" && a.AccountType == AccountType.Expense && !a.IsDeleted);
        if (expenseRoot is null)
            throw new InvalidOperationException("حساب جذر المصروفات (كود 5) غير موجود في شجرة الحسابات.");

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Code = AccountWasteExpense,
            NameAr = "مصروف الهالك/التوالف",
            NameEn = "Waste and Spoilage Expense",
            AccountType = AccountType.Expense,
            NormalBalance = NormalBalance.Debit,
            ParentAccountId = expenseRoot.Id,
            Description = "مصروف يُحمَّل عند إتلاف مخزون (انتهاء صلاحية / تلف / سرقة أو فقد) — يُنشأ تلقائياً عند أول سند إتلاف.",
            IsActive = true,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Account>().Add(account);
        return account;
    }

    private async Task<Account> GetAccountByCodeAsync(string code)
    {
        var account = await _context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي '{code}' غير موجود في شجرة الحسابات.");
        return account;
    }

    /// <summary>يتحقق أن مجموع كميات دُفعات (صنف، مخزن) == مجموع حركات المخزون لنفس (صنف، مخزن) — داخل نفس المعاملة.</summary>
    private async Task AssertBatchesConsistentAsync(Guid itemId, Guid warehouseId)
    {
        var batchTotal = await _context.Set<ItemBatch>()
            .Where(b => b.ItemId == itemId && b.WarehouseId == warehouseId)
            .SumAsync(b => (decimal?)b.Quantity) ?? 0m;

        var movementTotal = await _context.Set<StockMovement>()
            .Where(m => m.ItemId == itemId && m.WarehouseId == warehouseId)
            .SumAsync(m => (decimal?)m.Quantity) ?? 0m;

        if (batchTotal != movementTotal)
            throw new InvalidOperationException(
                $"تعارض تكامل دُفعات الإتلاف: مجموع الدُفعات ({batchTotal:N2}) ≠ رصيد حركات المخزون ({movementTotal:N2}).");
    }

    private static StockWriteOffDto MapToDto(StockWriteOff w) => new()
    {
        Id = w.Id,
        DocumentNumber = w.DocumentNumber,
        ItemId = w.ItemId,
        ItemCode = w.Item?.Code ?? "—",
        ItemNameAr = w.Item?.NameAr ?? "—",
        WarehouseId = w.WarehouseId,
        WarehouseNameAr = w.Warehouse?.NameAr ?? "—",
        BatchId = w.BatchId,
        BatchNumber = w.Batch?.BatchNumber,
        ExpiryDate = w.Batch?.ExpiryDate,
        Quantity = w.Quantity,
        Reason = (int)w.Reason,
        ReasonNameAr = GetReasonNameAr(w.Reason),
        OtherReasonText = w.OtherReasonText,
        Date = w.Date,
        UnitCost = w.UnitCost,
        Notes = w.Notes,
        CreatedByUserId = w.CreatedByUserId,
        JournalEntryId = w.JournalEntryId,
        EntryNumber = w.JournalEntry?.EntryNumber ?? "—",
        EntryDescription = w.JournalEntry?.Description ?? string.Empty
    };

    private static string ResolveStatus(DateOnly? expiry, DateOnly today)
    {
        if (expiry is null) return "Fine";
        if (expiry.Value < today) return "Expired";
        if (expiry.Value <= today.AddDays(7)) return "Expiring";
        return "Fine";
    }

    internal static string GetReasonNameAr(StockWriteOffReason reason) => reason switch
    {
        StockWriteOffReason.Expired => "منتهي الصلاحية",
        StockWriteOffReason.Damaged => "تالف/مكسور",
        StockWriteOffReason.Stolen => "سرقة أو فقد",
        StockWriteOffReason.Other => "أخرى",
        _ => "غير معروف"
    };

    private static string GetMonthNameAr(int month, int year)
    {
        var name = month switch
        {
            1 => "يناير",
            2 => "فبراير",
            3 => "مارس",
            4 => "أبريل",
            5 => "مايو",
            6 => "يونيو",
            7 => "يوليو",
            8 => "أغسطس",
            9 => "سبتمبر",
            10 => "أكتوبر",
            11 => "نوفمبر",
            12 => "ديسمبر",
            _ => string.Empty
        };
        return $"{name} {year}";
    }
}