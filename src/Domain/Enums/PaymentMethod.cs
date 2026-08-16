namespace ERPSystem.Domain.Enums;

/// <summary>
/// Determines how an expense voucher is settled.
/// - نقدًا (Cash): تُدفع من الصندوق فوراً.
/// - بنك (Bank): تُدفع عبر الحساب البنكي.
/// </summary>
public enum PaymentMethod
{
    /// <summary>نقدًا - Cash</summary>
    Cash = 1,

    /// <summary>بنك - Bank</summary>
    Bank = 2
}
