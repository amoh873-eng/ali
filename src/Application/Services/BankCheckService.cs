using ERPSystem.Application.DTOs.BankChecks;
using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// الشيكات البنكية (BankChecks) — أداة سداد مؤجلة بحالتين محاسبيتين:
///   1) عند الاستلام/الإصدار: «برسم التحصيل/السداد» (1102/2300) — ليست نقداً فورياً
///      لكنها تخفض رصيد العميل/المورد فوراً في الدفاتر.
///   2) عند التحصيل/الصرف: تتحول نقداً في «البنك» (1101).
///   3) عند الارتداد: يُعكس الأثر الأول ليصل الدين كما كان بالضبط.
/// كل تغيير حالة يرحّل قيداً متوازناً في معاملة ذرّية واحدة (نفس نمط RepCustody/CashDrawer).
/// </summary>
public class BankCheckService : IBankCheckService
{
    // الحسابات النظامية — تطابق إجباري مع بذر الهجرة وSeedSalesAccounts
    private const string AccountChecksReceivable = "1102"; // شيكات برسم التحصيل (أصل/مدين)
    private const string AccountChecksPayable = "2400";    // شيكات برسم السداد (خصم/دائن)
    private const string AccountBank = "1101";             // البنك (أصل/مدين) — عند التحصيل
    private const string AccountReceivable = "1200";       // ذمم العملاء (عودة الدين عند الارتداد)
    private const string AccountPayable = "2200";          // الموردون/الدائنون

    private readonly DbContext _context;
    private readonly IJournalEntryService _journalService;

    public BankCheckService(DbContext context, IJournalEntryService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    /// <summary>
    /// يحضّر شيكاً مُسجّلاً + قيد الفتح «برسم...» ضمن المعاملة الحالية (بلا SaveChanges —
    /// المستدعي يضيف الكيان ويحفظ مع معاملته ذرياً). يُستدعى من تسجيل الشاشة المستقلة
    /// ومن داخل معاملة الفاتورة (حيث تمرّر مثيل العميل/المورد نفسه فلا تُدار نسختان).
    /// </summary>
    public static async Task<BankCheck> PrepareRegisteredCheckAsync(
        DbContext context,
        IJournalEntryService journalService,
        CreateBankCheckDto dto,
        Customer? customer,
        Supplier? supplier)
    {
        if (dto.Amount <= 0m)
            throw new InvalidOperationException("مبلغ الشيك يجب أن يكون أكبر من صفر.");

        var received = dto.Direction == BankCheckDirection.ReceivedFromCustomer;
        if (received && customer is null)
            throw new InvalidOperationException("يجب تحديد العميل عند استلام شيك.");
        if (!received && supplier is null)
            throw new InvalidOperationException("يجب تحديد المورد عند إصدار شيك.");

        if (string.IsNullOrWhiteSpace(dto.CheckNumber))
            throw new InvalidOperationException("رقم الشيك مطلوب.");
        if (string.IsNullOrWhiteSpace(dto.BankName))
            throw new InvalidOperationException("اسم البنك مطلوب.");
        if (DateOnly.FromDateTime(dto.DueDate) < DateOnly.FromDateTime(dto.IssueDate))
            throw new InvalidOperationException("تاريخ الاستحقاق لا يمكن أن يسبق تاريخ كتابة الشيك.");

        var checksAccount = received
            ? await GetAccountByCodeAsync(context, AccountChecksReceivable)
            : await GetAccountByCodeAsync(context, AccountChecksPayable);
        var partyAccount = received
            ? await GetAccountByCodeAsync(context, AccountReceivable)
            : await GetAccountByCodeAsync(context, AccountPayable);

        var check = new BankCheck
        {
            Id = Guid.NewGuid(),
            CheckNumber = dto.CheckNumber.Trim(),
            BankName = dto.BankName.Trim(),
            BranchName = dto.BranchName,
            Direction = dto.Direction,
            CustomerId = received ? customer!.Id : null,
            SupplierId = received ? null : supplier!.Id,
            RelatedInvoiceId = dto.RelatedInvoiceId,
            Amount = dto.Amount,
            IssueDate = dto.IssueDate,
            DueDate = dto.DueDate,
            ReceivedOrIssuedDate = dto.ReceivedOrIssuedDate,
            Status = BankCheckStatus.Registered,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // فتح القيد: مستلم (مدين 1102 / دائن العميل) — مصدر (مدين المورد / دائن 2300)
        var reference = await NumberSequenceHelper.NextAsync(context, "CHK");
        var partyName = received ? customer!.NameAr : supplier!.NameAr;
        var actionLabel = received ? "استلام شيك من عميل" : "إصدار شيك لمورد";
        var entry = await journalService.PrepareEntryAsync(
            JournalEntryType.BankCheckReceipt,
            dto.ReceivedOrIssuedDate,
            reference,
            $"{actionLabel} {check.CheckNumber} - {check.BankName} - {partyName}",
            new List<JournalEntryLegInput>
            {
                new() { AccountId = checksAccount.Id, DebitAmount = dto.Amount, Note = $"شيك {check.CheckNumber}" },
                new() { AccountId = partyAccount.Id, CreditAmount = dto.Amount }
            });

        check.JournalEntryId = entry.Id;

        // تخفيض رصيد الطرف فوراً (الدين يعود فقط عند الارتداد)
        if (received)
        {
            customer!.CurrentBalance -= dto.Amount;
            customer!.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            supplier!.CurrentBalance -= dto.Amount;
            supplier!.UpdatedAt = DateTime.UtcNow;
        }

        return check;
    }
public async Task<BankCheckDto> RegisterAsync(CreateBankCheckDto dto)
    {
        Customer? customer = null;
        Supplier? supplier = null;
        if (dto.Direction == BankCheckDirection.ReceivedFromCustomer)
        {
            customer = await _context.Set<Customer>()
                .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && !c.IsDeleted);
            if (customer is null) throw new InvalidOperationException("العميل غير موجود.");
        }
        else
        {
            supplier = await _context.Set<Supplier>()
                .FirstOrDefaultAsync(s => s.Id == dto.SupplierId && !s.IsDeleted);
            if (supplier is null) throw new InvalidOperationException("المورد غير موجود.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var check = await PrepareRegisteredCheckAsync(_context, _journalService, dto, customer, supplier);
            _context.Set<BankCheck>().Add(check);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return MapToDto(check);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<BankCheck> LoadCheckOrThrowAsync(Guid id)
    {
        var check = await _context.Set<BankCheck>()
            .Include(t => t.Customer)
            .Include(t => t.Supplier)
            .Include(t => t.JournalEntry)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (check is null) throw new InvalidOperationException("الشيك غير موجود.");
        return check;
    }

    public async Task<BankCheckDto> DepositAsync(Guid id, DateTime depositDate)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var check = await LoadCheckOrThrowAsync(id);
            if (check.Direction != BankCheckDirection.ReceivedFromCustomer)
                throw new InvalidOperationException("الإيداع للتحصيل يخص الشيكات المستلمة من عملاء فقط.");
            if (check.Status == BankCheckStatus.Cleared)
                throw new InvalidOperationException("الشيك مُحصَّل بالفعل.");
            if (check.Status == BankCheckStatus.Bounced)
                throw new InvalidOperationException("الشيك مرتد — لا يمكن إيداعه.");

            // مرحلة وسيطة: لا أثر محاسبي — فقط تسجيل تاريخ الإيداع وتغيير الحالة
            check.Status = BankCheckStatus.Deposited;
            check.DepositDate = depositDate;
            check.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return MapToDto(check);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<BankCheckDto> ClearAsync(Guid id, DateTime clearedDate, string? notes = null)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var check = await LoadCheckOrThrowAsync(id);
            if (check.Status == BankCheckStatus.Cleared)
                throw new InvalidOperationException("الشيك مُحصَّل بالفعل.");
            if (check.Status == BankCheckStatus.Bounced)
                throw new InvalidOperationException("الشيك مرتد — لا يمكن تحصيله.");

            var received = check.Direction == BankCheckDirection.ReceivedFromCustomer;
            var bankAccount = await GetAccountByCodeAsync(AccountBank);
            var checksAccount = received
                ? await GetAccountByCodeAsync(AccountChecksReceivable)
                : await GetAccountByCodeAsync(AccountChecksPayable);

            var reference = await NumberSequenceHelper.NextAsync(_context, "CHK");
            var actionLabel = received ? "تحصيل شيك مستلم (نقد في البنك)" : "صرف شيك مصدر للمورد";
            var entry = await _journalService.PrepareEntryAsync(
                JournalEntryType.BankCheckClearance,
                clearedDate,
                reference,
                $"{actionLabel} {check.CheckNumber} - {check.BankName}",
                new List<JournalEntryLegInput>
                {
                    new() { AccountId = received ? bankAccount.Id : checksAccount.Id, DebitAmount = check.Amount, Note = $"شيك {check.CheckNumber}" },
                    new() { AccountId = received ? checksAccount.Id : bankAccount.Id, CreditAmount = check.Amount }
                });

            check.Status = BankCheckStatus.Cleared;
            check.ClearedDate = clearedDate;
            check.JournalEntryId = entry.Id;
            check.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return MapToDto(check);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
public async Task<BankCheckDto> BounceAsync(Guid id, DateTime bouncedDate, string? notes = null)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var check = await LoadCheckOrThrowAsync(id);
            if (check.Status == BankCheckStatus.Cleared)
                throw new InvalidOperationException("الشيك مُحصَّل — لا يمكن اعتباره مرتداً.");
            if (check.Status == BankCheckStatus.Bounced)
                throw new InvalidOperationException("الشيك مرتد بالفعل.");

            var received = check.Direction == BankCheckDirection.ReceivedFromCustomer;
            var partyAccount = received
                ? await GetAccountByCodeAsync(AccountReceivable)
                : await GetAccountByCodeAsync(AccountPayable);
            var checksAccount = received
                ? await GetAccountByCodeAsync(AccountChecksReceivable)
                : await GetAccountByCodeAsync(AccountChecksPayable);

            var reference = await NumberSequenceHelper.NextAsync(_context, "CHK");
            var actionLabel = received ? "ارتداد شيك مستلم (عودة دين العميل)" : "ارتداد شيك مصدر (عودة التزام المورد)";
            var entry = await _journalService.PrepareEntryAsync(
                JournalEntryType.BankCheckBounce,
                bouncedDate,
                reference,
                $"{actionLabel} {check.CheckNumber} - {check.BankName}",
                new List<JournalEntryLegInput>
                {
                    new() { AccountId = received ? partyAccount.Id : checksAccount.Id, DebitAmount = check.Amount, Note = $"ارتداد شيك {check.CheckNumber}" },
                    new() { AccountId = received ? checksAccount.Id : partyAccount.Id, CreditAmount = check.Amount }
                });

            check.Status = BankCheckStatus.Bounced;
            check.BouncedDate = bouncedDate;
            check.JournalEntryId = entry.Id;
            check.UpdatedAt = DateTime.UtcNow;

            // الدين يعود كما كان قبل فتح الشيك
            if (received)
            {
                check.Customer!.CurrentBalance += check.Amount;
                check.Customer!.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                check.Supplier!.CurrentBalance += check.Amount;
                check.Supplier!.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return MapToDto(check);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<BankCheckDto>> ListAsync(BankCheckDirection? direction = null)
    {
        var query = _context.Set<BankCheck>()
            .Include(t => t.Customer)
            .Include(t => t.Supplier)
            .Include(t => t.JournalEntry)
            .AsQueryable();

        if (direction is not null)
            query = query.Where(t => t.Direction == direction.Value);

        var rows = await query
            .OrderByDescending(t => t.ReceivedOrIssuedDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        var dtos = rows.Select(MapToDto).ToList();
        await FillRelatedInvoiceNumbersAsync(dtos);
        return dtos;
    }
public async Task<BankChecksReportDto> ReportAsync(
        DateTime? from, DateTime? to, BankCheckDirection? direction, BankCheckStatus? status)
    {
        var query = _context.Set<BankCheck>()
            .Include(t => t.Customer)
            .Include(t => t.Supplier)
            .Include(t => t.JournalEntry)
            .AsQueryable();

        if (direction is not null)
            query = query.Where(t => t.Direction == direction.Value);
        if (status is not null)
            query = query.Where(t => t.Status == status.Value);
        if (from.HasValue)
            query = query.Where(t => t.DueDate >= from.Value);
        if (to.HasValue)
        {
            var toExclusive = to.Value.Date.AddDays(1);
            query = query.Where(t => t.DueDate < toExclusive);
        }

        var rows = await query
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.ReceivedOrIssuedDate)
            .ToListAsync();

        var dtos = rows.Select(MapToDto).ToList();
        await FillRelatedInvoiceNumbersAsync(dtos);

        // ── المجاميع حسب الاتجاه (داخل نطاق الفلتر والاستحقاق) ──
        var report = new BankChecksReportDto();
        foreach (var r in dtos)
        {
            if (r.Direction == (int)BankCheckDirection.ReceivedFromCustomer)
                report.IncomingTotal += r.Amount;
            else
                report.OutgoingTotal += r.Amount;
        }

        // ── التوقع الشهري (تجميع حسب شهر الاستحقاق) ──
        var buckets = new List<BankCheckMonthlyBucketDto>();
        foreach (var r in dtos)
        {
            var bucket = buckets.FirstOrDefault(
                b => b.Year == r.DueDate.Year && b.Month == r.DueDate.Month);
            if (bucket is null)
            {
                bucket = new BankCheckMonthlyBucketDto { Year = r.DueDate.Year, Month = r.DueDate.Month };
                buckets.Add(bucket);
            }
            if (r.Direction == (int)BankCheckDirection.ReceivedFromCustomer)
                bucket.Incoming += r.Amount;
            else
                bucket.Outgoing += r.Amount;
            bucket.Net = bucket.Incoming - bucket.Outgoing;
        }
        report.Monthly = buckets
            .OrderBy(b => b.Year)
            .ThenBy(b => b.Month)
            .ToList();

        // ── المتأخرة: استحقاقها مضى وحالتها ما زالت قيد المتابعة ──
        var todayStart = DateOnly.FromDateTime(DateTime.Today);
        report.Overdue = dtos
            .Where(r => DateOnly.FromDateTime(r.DueDate) < todayStart
                && (r.Status == (int)BankCheckStatus.Registered || r.Status == (int)BankCheckStatus.Deposited))
            .OrderBy(r => r.DueDate)
            .ToList();

        report.Rows = dtos;
        return report;
    }

    /// <summary>تعبئة أرقام الفواتير المرتبطة (مبيعات ثم مشتريات — دفعات استعلام مقيدة).</summary>
    private async Task FillRelatedInvoiceNumbersAsync(List<BankCheckDto> rows)
    {
        var relatedIds = new List<Guid>();
        foreach (var r in rows)
            if (r.RelatedInvoiceId.HasValue)
                relatedIds.Add(r.RelatedInvoiceId.Value);
        if (relatedIds.Count == 0) return;

        var sales = await _context.Set<SalesInvoice>()
            .Where(i => relatedIds.Contains(i.Id))
            .ToListAsync();
        var salesIds = sales.Select(i => i.Id).ToList();
        var purchaseIds = relatedIds.Where(id => !salesIds.Contains(id)).ToList();

        var byId = new Dictionary<Guid, string>();
        foreach (var s in sales) byId[s.Id] = s.InvoiceNumber;

        if (purchaseIds.Count > 0)
        {
            var purchases = await _context.Set<PurchaseInvoice>()
                .Where(i => purchaseIds.Contains(i.Id))
                .ToListAsync();
            foreach (var p in purchases) byId[p.Id] = p.InvoiceNumber;
        }

        foreach (var r in rows)
        {
            if (r.RelatedInvoiceId.HasValue)
                r.RelatedInvoiceNumber = byId.TryGetValue(r.RelatedInvoiceId.Value, out var num) ? num : null;
        }
    }

    private static async Task<Account> GetAccountByCodeAsync(DbContext context, string code)
    {
        var account = await context.Set<Account>()
            .FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted);
        if (account is null)
            throw new InvalidOperationException($"الحساب النظامي '{code}' غير موجود في شجرة الحسابات.");
        return account;
    }

    private async Task<Account> GetAccountByCodeAsync(string code) => await GetAccountByCodeAsync(_context, code);

    private static BankCheckDto MapToDto(BankCheck t)
        => new()
        {
            Id = t.Id,
            CheckNumber = t.CheckNumber,
            BankName = t.BankName,
            BranchName = t.BranchName,
            Direction = (int)t.Direction,
            CustomerId = t.CustomerId,
            SupplierId = t.SupplierId,
            PartyName = t.Customer is not null ? t.Customer.NameAr : (t.Supplier is not null ? t.Supplier.NameAr : "—"),
            RelatedInvoiceId = t.RelatedInvoiceId,
            Amount = t.Amount,
            IssueDate = t.IssueDate,
            DueDate = t.DueDate,
            ReceivedOrIssuedDate = t.ReceivedOrIssuedDate,
            Status = (int)t.Status,
            DepositDate = t.DepositDate,
            ClearedDate = t.ClearedDate,
            BouncedDate = t.BouncedDate,
            Notes = t.Notes,
            JournalEntryId = t.JournalEntryId,
            EntryNumber = t.JournalEntry?.EntryNumber ?? "—"
        };
}