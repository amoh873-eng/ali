using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// شيك بنكي (BankCheck) — أداة سداد مؤجلة تمر بحالتين محاسبيتين:
/// عند الاستلام/الإصدار تُسجَّل كأصل/التزام «برسم التحصيل/السداد» (ليست نقداً فورياً)،
/// وعند الاستحقاق والتحصيل تتحول نقداً في البنك، وعند الارتداد يُعكس الأثر الأول
/// ليصل رصيد العميل/المورد إلى ما كان عليه بالضبط.
/// </summary>
public class BankCheck
{
    public Guid Id { get; set; }

    /// <summary>رقم الشيك المطبوع (المعرّف الفعلي لدى البنك).</summary>
    public string CheckNumber { get; set; } = string.Empty;

    /// <summary>اسم البنك.</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>فرع البنك (اختياري — بعض الشيكات تحدد الفرع).</summary>
    public string? BranchName { get; set; }

    /// <summary>استلام من عميل أم إصدار لصالح مورد.</summary>
    public BankCheckDirection Direction { get; set; }

    /// <summary>العميل (عند الاستلام) — مرجع تحليلي.</summary>
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>المورد (عند الإصدار) — مرجع تحليلي.</summary>
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>
    /// الفاتورة المرتبطة إن كان الشيك يسوّي فاتورة بعينها (ربط واحد-لواحد)؛
    /// وإلا فهو تسوية عامة على الحساب. يبقى معرفاً بلا قيد DB لاحتمال أحدهما (مبيعات/مشتريات).
    /// </summary>
    public Guid? RelatedInvoiceId { get; set; }

    /// <summary>مبلغ الشيك (أكبر من صفر).</summary>
    public decimal Amount { get; set; }

    /// <summary>تاريخ كتابة الشيك نفسه (المدوّن عليه).</summary>
    public DateTime IssueDate { get; set; }

    /// <summary>تاريخ الاستحقاق — التاريخ الذي يصبح عنده قابلاً للتحصيل (محرك تقرير التدفق).</summary>
    public DateTime DueDate { get; set; }

    /// <summary>تاريخ الدخول/الخروج الفعلي لدى المنشأة (يختلف عن IssueDate في الأغلب).</summary>
    public DateTime ReceivedOrIssuedDate { get; set; }

    /// <summary>الحالة الحالية في دورة الحياة.</summary>
    public BankCheckStatus Status { get; set; } = BankCheckStatus.Registered;

    /// <summary>تاريخ الإيداع للتحصيل (اختياري — للمستلم فقط).</summary>
    public DateTime? DepositDate { get; set; }

    /// <summary>تاريخ التحصيل/الصرف المؤكد (قد يختلف عن الاستحقاق عند تأخّر التسوية).</summary>
    public DateTime? ClearedDate { get; set; }

    /// <summary>تاريخ الارتداد إن حدث.</summary>
    public DateTime? BouncedDate { get; set; }

    /// <summary>ملاحظة حرة اختيارية.</summary>
    public string? Notes { get; set; }

    /// <summary>معرّف القيد المحاسبي المرتبط بآخر حدث (فتح/تحصيل/ارتداد).</summary>
    public Guid JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}