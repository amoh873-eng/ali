using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.BankChecks;

/// <summary>بيانات تسجيل شيك (استلام من عميل أو إصدار لمورد) — عبر الشاشة المستقلة أو ضمن الفاتورة.</summary>
public class CreateBankCheckDto
{
    /// <summary>رقم الشيك المطبوع (مطلوب).</summary>
    public string CheckNumber = string.Empty;

    /// <summary>اسم البنك (مطلوب).</summary>
    public string BankName = string.Empty;

    /// <summary>فرع البنك (اختياري).</summary>
    public string? BranchName;

    /// <summary>استلام من عميل أم إصدار لمورد.</summary>
    public BankCheckDirection Direction = BankCheckDirection.ReceivedFromCustomer;

    /// <summary>العميل (مطلوب عند الاستلام).</summary>
    public Guid? CustomerId;

    /// <summary>المورد (مطلوب عند الإصدار).</summary>
    public Guid? SupplierId;

    /// <summary>الفاتورة المرتبطة إن وُجدت (ربط واحد-لواحد).</summary>
    public Guid? RelatedInvoiceId;

    /// <summary>مبلغ الشيك (أكبر من صفر).</summary>
    public decimal Amount;

    /// <summary>تاريخ كتابة الشيك (المدوّن عليه).</summary>
    public DateTime IssueDate = DateTime.Now;

    /// <summary>تاريخ الاستحقاق — محرك تقرير التدفق.</summary>
    public DateTime DueDate = DateTime.Now.AddDays(30);

    /// <summary>تاريخ الدخول/الخروج الفعلي.</summary>
    public DateTime ReceivedOrIssuedDate = DateTime.Now;

    /// <summary>ملاحظة اختيارية.</summary>
    public string? Notes;
}

/// <summary>صف شيك في القوائم والتقرير (مع أسماء الأطراف ورقم الفاتورة المرتبطة).</summary>
public class BankCheckDto
{
    public Guid Id;
    public string CheckNumber;
    public string BankName;
    public string? BranchName;
    public int Direction;
    public Guid? CustomerId;
    public Guid? SupplierId;
    public string? PartyName;
    public Guid? RelatedInvoiceId;
    public string? RelatedInvoiceNumber;
    public decimal Amount;
    public DateTime IssueDate;
    public DateTime DueDate;
    public DateTime ReceivedOrIssuedDate;
    public int Status;
    public DateTime? DepositDate;
    public DateTime? ClearedDate;
    public DateTime? BouncedDate;
    public string? Notes;
    public Guid JournalEntryId;
    public string EntryNumber;
}

/// <summary>مجموعة شهرية (سنة/شهر) لمبلغ الاستحقاق الوارد vs الصادر.</summary>
public class BankCheckMonthlyBucketDto
{
    public int Year;
    public int Month;
    public decimal Incoming;
    public decimal Outgoing;
    public decimal Net;
}

/// <summary>نتيجة تقرير الشيكات: مجاميع + توقعات شهرية + المتأخرة + الصفوف المفلترة.</summary>
public class BankChecksReportDto
{
    public decimal IncomingTotal;
    public decimal OutgoingTotal;
    public List<BankCheckDto> Overdue = new();
    public List<BankCheckMonthlyBucketDto> Monthly = new();
    public List<BankCheckDto> Rows = new();
}