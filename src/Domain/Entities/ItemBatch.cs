namespace ERPSystem.Domain.Entities;

/// <summary>
/// دفعة/شحنة مخزون (batch/lot) لتتبّع تواريخ الإنتاج والانتهاء اختيارياً.
/// كيان جديد مستقل عن StockBatch (الموروث من عمل سابق) — يُستخدم فقط للأصناف التي
/// فعّلت حقل Item.TracksBatches. كل دفعة تخص صنفاً واحداً ومخزناً واحداً.
/// </summary>
public class ItemBatch
{
    /// <summary>المعرّف الفريد للدفعة.</summary>
    public Guid Id { get; set; }

    /// <summary>مفتاح أجنبي إلى الصنف (Item).</summary>
    public Guid ItemId { get; set; }

    /// <summary>كائن التنقّل إلى الصنف.</summary>
    public Item? Item { get; set; }

    /// <summary>مفتاح أجنبي إلى المخزن (Warehouse) — الدُفعة تخص مخزناً محدداً.</summary>
    public Guid WarehouseId { get; set; }

    /// <summary>كائن التنقّل إلى المخزن.</summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>رقم الدُفعة/اللوت من المورد (اختياري إن لم يوجد رقم فعلي).</summary>
    public string? BatchNumber { get; set; }

    /// <summary>تاريخ الإنتاج (اختياري).</summary>
    public DateOnly? ProductionDate { get; set; }

    /// <summary>تاريخ انتهاء الصلاحية — مطلوب عند تفعيل Item.TracksBatches.</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>الكمية المتبقية من هذه الدُفعة تحديداً.</summary>
    public decimal Quantity { get; set; }

    /// <summary>تاريخ استلام الدُفعة.</summary>
    public DateTime ReceivedDate { get; set; }

    /// <summary>
    /// مرجع فاتورة الشراء التي أدخلت هذه الدُفعة — إن كانت دُفعة مستلمة من مشتريات.
    /// (أُدخلت عبر صفوف v؛ وقد تُنشأ الدفعة أيضاً من حركة وارد أخرى بلا فاتورة ⇒ null).
    /// </summary>
    public Guid? PurchaseInvoiceId { get; set; }

    /// <summary>كائن التنقّل إلى فاتورة الشراء (اختياري).</summary>
    public PurchaseInvoice? PurchaseInvoice { get; set; }

    /// <summary>آخر تاريخ أُرسل فيه تنبيه انتهاء لهذه الدُفعة (لتفادي التكرار).</summary>
    public DateTime? LastExpiryNotifiedAt { get; set; }
}