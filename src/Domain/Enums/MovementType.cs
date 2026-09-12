namespace ERPSystem.Domain.Enums;

/// <summary>
/// Represents the types of inventory stock movements.
/// Each type has a fixed effect on stock:
/// - واردة (In): PurchaseReceipt, OpeningBalance, AdjustmentIn, TransferIn → تزيد الرصيد
/// - صادرة (Out): SalesIssue, AdjustmentOut, TransferOut → تنقص الرصيد
/// </summary>
public enum MovementType
{
    /// <summary>وارد مشتريات - Purchase receipt (In)</summary>
    PurchaseReceipt = 1,

    /// <summary>صادر مبيعات - Sales issue (Out)</summary>
    SalesIssue = 2,

    /// <summary>رصيد افتتاحي - Opening balance (In)</summary>
    OpeningBalance = 3,

    /// <summary>تسوية إضافة (جرد) - Adjustment increase (In)</summary>
    AdjustmentIn = 4,

    /// <summary>تسوية خصم (جرد) - Adjustment decrease (Out)</summary>
    AdjustmentOut = 5,

    /// <summary>وارد تحويل بين المخازن - Transfer in (In)</summary>
    TransferIn = 6,

    /// <summary>صادر تحويل بين المخازن - Transfer out (Out)</summary>
    TransferOut = 7,

    /// <summary>وارد مردود مبيعات - Sales return in (In)</summary>
    SalesReturnIn = 8,

    /// <summary>صادر مردود مشتريات - Purchase return out (Out)</summary>
    PurchaseReturnOut = 9,

    /// <summary>صادر إتلاف مخزون - Stock write-off (Out) — من سند الإتلاف StockWriteOff</summary>
    WriteOff = 10
}
