using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Payroll;

public class PayrollRunDto
{
    public Guid Id { get; set; }
    public string RunNumber { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PayrollRunStatus Status { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNet { get; set; }
    public Guid? JournalEntryId { get; set; }
    public List<PayrollRunLineDto> Lines { get; set; } = new();
}

public class PayrollRunLineDto
{
    public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeNameAr { get; set; }
    public string? EmployeeNumber { get; set; }
    public decimal BasicSalary { get; set; }
    public int UnpaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal NetPay { get; set; }
}
