namespace ERPSystem.Domain.Entities;

/// <summary>
/// دفعة/شحنة مخزون (bacth/lot) — طبقة إضافية اختيارية لتتبع تواريخ الانتهاء.
/// لا يحل محل Item.CurrentStock (الرصيد الكلي السريع يبقى كما هو)، بل يُضاف فقط
/// للصنف الذي فعّل TracksExpiry. نفس الصنف قد يملك عدة دفعات بتاريخ انتهاء مختلف.
/// البيع يُخصم من الأقدم انتهاءً أولاً (FEFO) عبر StockBatchHelper.
/// </summary>
public class StockBatch
{
    /// <summary>المعرّف الفريد للدفعة.</summary>
    public Guid Id { get; set; }

    /// <summary>مفتاح أجنبي إلى الصنف (Item).</summary>
    public Guid ItemId { get; set; }

    /// <summary>كائن التنقّل إلى الصنف.</summary>
    public Item? Item { get; set; }

    /// <summary>مفتاح أجنبي إلى المخزن (Warehouse).</summary>
    public Guid WarehouseId { get; set; }

    /// <summary>كائن التنقّل إلى المخزن.</summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>رقم الدفعة/الشحنة من المورد (اختياري، كود اللوت).</summary>
    public string? BatchNumber { get; set; }

    /// <summary>تاريخ انتهاء صلاحية الدفعة (اختياري).</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>الكمية المتبقية في هذه الدفعة (أكبر من صفر للدفعات الصالحة).</summary>
    public decimal Quantity { get; set; }

    /// <summary>تاريخ استلام هذه الدفعة.</summary>
    public DateTime ReceivedDate { get; set; }

    /// <summary>
    /// آخر تاريخ أُرسل فيه تنبيه انتهاء صلاحية لهذه الدفعة (لتفادي التكرار اليومي).
    /// خفي — يُستخدم بواسطة خدمة الفحص الخلفية فقط.
    /// </summary>
    public DateTime? LastExpiryNotifiedAt { get; set; }
}