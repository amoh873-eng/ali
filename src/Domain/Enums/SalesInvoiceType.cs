namespace ERPSystem.Domain.Enums;

/// <summary>
/// Determines how a sales invoice is settled.
/// - نقدي (Cash): يُسدد كاملاً لحظة البيع → حساب الصندوق/النقدية
/// - آجل (OnAccount): يترتب على العميل مبلغ مستحق → حساب العملاء/المدينون
/// هذا التمييز حاسم لأنه يحدد أي حساب مدين يُستخدم في القيد المحاسبي:
/// لو كان البيع نقداً نُدين "الصندوق"، وإذا كان آجلاً نُدين "العملاء".
/// </summary>
public enum SalesInvoiceType
{
    /// <summary>فاتورة نقدية - تُسدد فوراً (Cash Sale)</summary>
    Cash = 1,

    /// <summary>فاتورة آجلة - مبلغ مستحق على العميل (Credit/On-Account Sale)</summary>
    OnAccount = 2
}
