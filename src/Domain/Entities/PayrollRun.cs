using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// دورة رواتب لفترة محددة — تمثّل دفعة رواتب جماعية واحدة.
/// لماذا نحفظ الإجماليات (TotalGross/TotalDeductions/TotalNet) على مستوى الدورة؟
/// لسهولة العرض وتوليد القيود المحاسبية دون الحاجة لجمع كل سطور الدورة في كل مرة،
/// مع ضمان أن المجموع يساوي فعلياً مجموع بنود الدورة عند الحفظ.
/// </summary>
public class PayrollRun : BaseEntity
{
    /// <summary>
    /// رقم الدورة الفريد (مثل PR-202603-0001) — يُولّد عبر NumberSequence بذرياً.
    /// </summary>
    public string RunNumber { get; set; } = string.Empty;

    /// <summary>
    /// بداية فترة الرواتب (شاملة).
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// نهاية فترة الرواتب (شاملة).
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// حالة الدورة (مسودة/معتمدة/مرحّلة).
    /// </summary>
    public PayrollRunStatus Status { get; set; }

    /// <summary>
    /// مجموع الرواتب الأساسية (مجموع BasicSalary لكل الموظفين).
    /// </summary>
    public decimal TotalGross { get; set; }

    /// <summary>
    /// مجموع الاستقطاعات (مجموع UnpaidLeaveDeduction + OtherDeductions).
    /// </summary>
    public decimal TotalDeductions { get; set; }

    /// <summary>
    /// صافي الرواتب (مجموع NetPay).
    /// </summary>
    public decimal TotalNet { get; set; }

    /// <summary>
    /// معرّف القيد المحاسبي المرتبط (يُعيّن عند الترحيل إلى Paid).
    /// </summary>
    public Guid? JournalEntryId { get; set; }

    /// <summary>
    /// القيد المحاسبي المرتبط.
    /// </summary>
    public JournalEntry? JournalEntry { get; set; }

    /// <summary>
    /// رمز التزامن المتفائل (RowVersion) — يكشف التعديلات المفقودة عند تزامن طلبين.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// بنود الدورة (سطر واحد لكل موظف نشط).
    /// </summary>
    public ICollection<PayrollRunLine> Lines { get; set; } = new List<PayrollRunLine>();
}
