namespace ERPSystem.Domain.Enums;

/// <summary>
/// يحدد كيفية تسوية فاتورة المشتريات:
/// - نقدي (Cash): تُسدد فوراً → حساب الصندوق/النقدية
/// - آجل (OnAccount): يترتب على المورد مبلغ مستحق → حساب الموردين/الدائنون
/// </summary>
public enum PurchaseInvoiceType
{
    /// <summary>فاتورة نقدية - تُسدد فوراً (Cash Purchase)</summary>
    Cash = 1,

    /// <summary>فاتورة آجلة - مبلغ مستحق للمورد (Credit/On-Account Purchase)</summary>
    OnAccount = 2
}