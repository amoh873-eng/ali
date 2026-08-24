using ERPSystem.Application.DTOs.Payroll;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// خدمة الرواتب — توليد الدورة + الاعتماد + الترحيل المحاسبي.
/// لماذا معاملة Serializable + فحص تداخل مقفل؟ لمنع توليد دورتين متداخلتين لنفس الفترة
/// عند ضغط مستخدمين على "توليد" في نفس اللحظة — نفس فلسفة StockAvailabilityHelper
/// (قفل صريح حتى commit)، مع حاجز ثانٍ على مستوى قاعدة البيانات عبر الفهرس الفريد.
/// </summary>
public partial class PayrollService : IPayrollService
{
    private const string AccExpense = "5200";
    private const string AccPayable = "2300";
    private readonly DbContext _ctx;
    private readonly IJournalEntryService _journal;
    private readonly IAttendanceService _att;
    public PayrollService(DbContext ctx, IJournalEntryService j, IAttendanceService a) { _ctx = ctx; _journal = j; _att = a; }

    public async Task<PayrollRunDto> GenerateRunAsync(DateTime s, DateTime e)
    {
        s = s.Date; e = e.Date;
        if (e < s) throw new InvalidOperationException("تاريخ نهاية الفترة لا يسبق بدايتها");
        // معاملة Serializable تضمن أن الفحص + الإدراج ذريان — لا نافذة سباق.
        await using var tx = await _ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        // فحص التداخل: أي دورة تتداخل مع [s,e] ولو بيوم واحد تُرفض. نستخدم AnyAsync داخل نفس المعاملة المقفلة.
        if (await _ctx.Set<PayrollRun>().AnyAsync(p => !p.IsDeleted && p.PeriodStart <= e && p.PeriodEnd >= s))
            throw new InvalidOperationException("توجد دورة رواتب متداخلة مع نفس الفترة مسبقاً");
        var emps = await _ctx.Set<Employee>().Where(x => !x.IsDeleted && x.Status == EmployeeStatus.Active).ToListAsync();
        if (emps.Count == 0) throw new InvalidOperationException("لا يوجد موظفون نشطون لتوليد دورة رواتب");
        // ترقيم آمن ضد التزامن عبر NumberSequenceHelper (MERGE WITH HOLDLOCK) — لا نستخدم قراءة/كتابة يدوية للـ Sequence.
        var num = await NumberSequenceHelper.NextAsync(_ctx, "PR");
        var days = (e - s).Days + 1;
        var run = new PayrollRun { Id = Guid.NewGuid(), RunNumber = num, PeriodStart = s, PeriodEnd = e, Status = PayrollRunStatus.Draft, CreatedAt = DateTime.UtcNow };
        _ctx.Set<PayrollRun>().Add(run);
        foreach (var emp in emps)
        {
            var ud = await _att.GetUnpaidDeductionDaysAsync(emp.Id, DateOnly.FromDateTime(s), DateOnly.FromDateTime(e));
            var rate = days > 0 ? emp.BasicSalary / days : 0;
            var ded = Math.Round(rate * ud, 2);
            var net = Math.Round(emp.BasicSalary - ded, 2); if (net < 0) net = 0;
            var line = new PayrollRunLine { Id = Guid.NewGuid(), PayrollRunId = run.Id, EmployeeId = emp.Id, BasicSalary = emp.BasicSalary, UnpaidLeaveDays = ud, UnpaidLeaveDeduction = ded, OtherDeductions = 0, OtherAllowances = 0, NetPay = net, CreatedAt = DateTime.UtcNow };
            _ctx.Set<PayrollRunLine>().Add(line); run.Lines.Add(line);
        }
        run.TotalGross = run.Lines.Sum(x => x.BasicSalary);
        run.TotalDeductions = run.Lines.Sum(x => x.UnpaidLeaveDeduction + x.OtherDeductions);
        run.TotalNet = run.Lines.Sum(x => x.NetPay);
        try
        {
            await ConcurrencyHelper.SaveChangesWithFriendlyErrorAsync(_ctx);
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsUnique(ex)) { await tx.RollbackAsync(); throw new InvalidOperationException("توجد دورة رواتب متداخلة مع نفس الفترة مسبقاً"); }
        catch (InvalidOperationException) { await tx.RollbackAsync(); throw; }
        return await GetRunByIdAsync(run.Id) ?? Map(run);
    }

    private static bool IsUnique(DbUpdateException ex) => ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;
    private async Task<Account> GetAcc(string code) => await _ctx.Set<Account>().FirstOrDefaultAsync(a => a.Code == code && !a.IsDeleted) ?? throw new InvalidOperationException($"الحساب '{code}' غير موجود");
    private static PayrollRunDto Map(PayrollRun p) => new() { Id = p.Id, RunNumber = p.RunNumber, PeriodStart = p.PeriodStart, PeriodEnd = p.PeriodEnd, Status = p.Status, TotalGross = p.TotalGross, TotalDeductions = p.TotalDeductions, TotalNet = p.TotalNet, JournalEntryId = p.JournalEntryId, Lines = p.Lines.Select(MapLine).OrderBy(x => x.EmployeeNameAr).ToList() };
    private static PayrollRunLineDto MapLine(PayrollRunLine l) => new() { Id = l.Id, PayrollRunId = l.PayrollRunId, EmployeeId = l.EmployeeId, EmployeeNameAr = l.Employee?.NameAr, EmployeeNumber = l.Employee?.EmployeeNumber, BasicSalary = l.BasicSalary, UnpaidLeaveDays = l.UnpaidLeaveDays, UnpaidLeaveDeduction = l.UnpaidLeaveDeduction, OtherDeductions = l.OtherDeductions, OtherAllowances = l.OtherAllowances, NetPay = l.NetPay };
}
