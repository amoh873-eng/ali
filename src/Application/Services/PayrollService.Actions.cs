using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Application.DTOs.Payroll;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

public partial class PayrollService
{
    public async Task<PayrollRunDto> ApproveRunAsync(Guid runId)
    {
        var run = await _ctx.Set<Domain.Entities.PayrollRun>().Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == runId && !p.IsDeleted) ?? throw new InvalidOperationException("دورة الرواتب غير موجودة");
        if (run.Status != PayrollRunStatus.Draft) throw new InvalidOperationException("لا يمكن اعتماد دورة ليست في حالة مسودة");
        run.Status = PayrollRunStatus.Approved; run.UpdatedAt = DateTime.UtcNow;
        // حماية RowVersion: أي تعديل متزامن سيرمي DbUpdateConcurrencyException → رسالة مفهومة
        await ConcurrencyHelper.SaveChangesWithFriendlyErrorAsync(_ctx);
        return Map(run);
    }

    public async Task<PayrollRunDto> PostRunToJournalAsync(Guid runId)
    {
        await using var tx = await _ctx.Database.BeginTransactionAsync();
        var run = await _ctx.Set<Domain.Entities.PayrollRun>().Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == runId && !p.IsDeleted) ?? throw new InvalidOperationException("دورة الرواتب غير موجودة");
        if (run.Status != PayrollRunStatus.Approved) throw new InvalidOperationException("لا يمكن ترحيل دورة ليست في حالة معتمدة — يجب اعتمادها أولاً");
        if (run.JournalEntryId.HasValue) throw new InvalidOperationException("تم ترحيل هذه الدورة مسبقاً — لا يمكن الترحيل مرتين");
        var exp = await GetAcc(AccExpense);
        var pay = await GetAcc(AccPayable);
        // قيد متوازن: مدين مصروف = TotalGross+Allowances، دائن مستحق = TotalNet، والفرق استقطاعات كدائن على نفس المصروف
        var legs = new List<JournalEntryLegInput>
        {
            new() { AccountId = exp.Id, DebitAmount = run.TotalGross + run.Lines.Sum(l => l.OtherAllowances) },
            new() { AccountId = pay.Id, CreditAmount = run.TotalNet }
        };
        if (run.TotalDeductions > 0) legs.Add(new JournalEntryLegInput { AccountId = exp.Id, CreditAmount = run.TotalDeductions });
        var entry = await _journal.PrepareEntryAsync(JournalEntryType.Payroll, run.PeriodEnd, run.RunNumber, $"رواتب الفترة {run.PeriodStart:yyyy-MM-dd} إلى {run.PeriodEnd:yyyy-MM-dd}", legs);
        run.JournalEntryId = entry.Id; run.Status = PayrollRunStatus.Paid; run.UpdatedAt = DateTime.UtcNow;
        try
        {
            await ConcurrencyHelper.SaveChangesWithFriendlyErrorAsync(_ctx);
            await tx.CommitAsync();
        }
        catch (InvalidOperationException) { await tx.RollbackAsync(); throw; }
        return Map(run);
    }

    public async Task<List<PayrollRunDto>> GetRunsAsync()
    {
        var runs = await _ctx.Set<Domain.Entities.PayrollRun>().Include(p => p.Lines).ThenInclude(l => l.Employee).OrderByDescending(p => p.PeriodStart).ToListAsync();
        return runs.Select(Map).ToList();
    }

    public async Task<PayrollRunDto?> GetRunByIdAsync(Guid runId)
    {
        var run = await _ctx.Set<Domain.Entities.PayrollRun>().Include(p => p.Lines).ThenInclude(l => l.Employee).FirstOrDefaultAsync(p => p.Id == runId && !p.IsDeleted);
        return run is null ? null : Map(run);
    }

    public async Task<PayrollRunLineDto> UpdateLineAsync(Guid lineId, decimal otherDeductions, decimal otherAllowances)
    {
        if (otherDeductions < 0 || otherAllowances < 0) throw new InvalidOperationException("المبالغ لا يمكن أن تكون سالبة");
        var line = await _ctx.Set<Domain.Entities.PayrollRunLine>().Include(l => l.PayrollRun).FirstOrDefaultAsync(l => l.Id == lineId && !l.IsDeleted) ?? throw new InvalidOperationException("سطر الرواتب غير موجود");
        if (line.PayrollRun is null || line.PayrollRun.Status != PayrollRunStatus.Draft) throw new InvalidOperationException("لا يمكن تعديل سطر إلا عندما تكون الدورة في حالة مسودة");
        line.OtherDeductions = Math.Round(otherDeductions, 2);
        line.OtherAllowances = Math.Round(otherAllowances, 2);
        line.NetPay = Math.Round(line.BasicSalary - line.UnpaidLeaveDeduction - line.OtherDeductions + line.OtherAllowances, 2);
        if (line.NetPay < 0) line.NetPay = 0;
        line.UpdatedAt = DateTime.UtcNow;
        var run = line.PayrollRun;
        var all = await _ctx.Set<Domain.Entities.PayrollRunLine>().Where(l => l.PayrollRunId == run.Id && !l.IsDeleted).ToListAsync();
        var tgt = all.First(l => l.Id == lineId); tgt.OtherDeductions = line.OtherDeductions; tgt.OtherAllowances = line.OtherAllowances; tgt.NetPay = line.NetPay;
        run.TotalDeductions = all.Sum(l => l.UnpaidLeaveDeduction + l.OtherDeductions);
        run.TotalNet = all.Sum(l => l.NetPay);
        run.UpdatedAt = DateTime.UtcNow;
        await ConcurrencyHelper.SaveChangesWithFriendlyErrorAsync(_ctx);
        return MapLine(line);
    }

    public async Task<List<PayrollRunLineDto>> GetEmployeeHistoryAsync(Guid employeeId)
    {
        var lines = await _ctx.Set<Domain.Entities.PayrollRunLine>().Include(l => l.PayrollRun).Where(l => l.EmployeeId == employeeId && !l.IsDeleted).OrderByDescending(l => l.PayrollRun!.PeriodStart).ToListAsync();
        return lines.Select(MapLine).ToList();
    }
}
