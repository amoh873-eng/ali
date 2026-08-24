using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// سطر واحد داخل دورة رواتب — يمثل تفصيل راتب موظف واحد.
/// لماذا \"لقطة\" (snapshot) للراتب الأساسي؟ لأن راتب الموظف قد يتغير لاحقاً،
/// لكن دورة رواتب معالجة تاريخياً يجب أن تبقى ثابتة كما كانت وقت المعالجة.
/// بدون اللقطة، أي تعديل لاحق على Employee.BasicSalary سيُشوّه سجلاً محاسبياً قديماً.
/// </summary>
public class PayrollRunLine : BaseEntity
{
    /// <summary>
    /// معرّف دورة الرواتب الأب.
    /// </summary>
    public Guid PayrollRunId { get; set; }

    /// <summary>
    /// دورة الرواتب الأب.
    /// </summary>
    public PayrollRun? PayrollRun { get; set; }

    /// <summary>
    /// معرّف الموظف.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// الموظف المرتبط.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// الراتب الأساسي كما كان وقت توليد الدورة (لقطة من Employee.BasicSalary).
    /// </summary>
    public decimal BasicSalary { get; set; }

    /// <summary>
    /// عدد أيام الإجازة غير المدفوعة / الغياب غير المبرر ضمن فترة الدورة.
    /// </summary>
    public int UnpaidLeaveDays { get; set; }

    /// <summary>
    /// مبلغ الخصم الناتج عن الإجازات غير المدفوعة (محسوب: BasicSalary / أيام الفترة × UnpaidLeaveDays).
    /// </summary>
    public decimal UnpaidLeaveDeduction { get; set; }

    /// <summary>
    /// استقطاعات أخرى (جزاءات، سلف، ...).
    /// </summary>
    public decimal OtherDeductions { get; set; }

    /// <summary>
    /// بدلات إضافية (مكافآت، بدل نقل، ...).
    /// </summary>
    public decimal OtherAllowances { get; set; }

    /// <summary>
    /// صافي الراتب: BasicSalary - UnpaidLeaveDeduction - OtherDeductions + OtherAllowances
    /// </summary>
    public decimal NetPay { get; set; }
}
