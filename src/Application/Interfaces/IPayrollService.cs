using ERPSystem.Application.DTOs.Payroll;

namespace ERPSystem.Application.Interfaces;

public interface IPayrollService
{
    Task<PayrollRunDto> GenerateRunAsync(DateTime periodStart, DateTime periodEnd);
    Task<PayrollRunDto> ApproveRunAsync(Guid runId);
    Task<PayrollRunDto> PostRunToJournalAsync(Guid runId);
    Task<List<PayrollRunDto>> GetRunsAsync();
    Task<PayrollRunDto?> GetRunByIdAsync(Guid runId);
    Task<PayrollRunLineDto> UpdateLineAsync(Guid lineId, decimal otherDeductions, decimal otherAllowances);
    Task<List<PayrollRunLineDto>> GetEmployeeHistoryAsync(Guid employeeId);
}
