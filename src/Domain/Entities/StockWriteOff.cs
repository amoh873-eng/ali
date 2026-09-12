using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// سند إتلاف مخزون (Stock Write-Off) — تسجيل خروج كمية من المخزون لأسباب
/// (انتهاء صلاحية / تلف / سرقة أو فقد / أخرى) مع أثر محاسبي آلي متوازن:
///   - مدين: "مصروف الهالك/التوالف" — دائن: "المخزون" (نفس حساب المخزون 1300 في النظام).
///   - يتم ترحيله في معاملة واحدة ذرية: الحركة + خصم الدُفعة + السند + القيد معاً.
///
/// ملاحظة التقييم: UnitCost تُنسَخ من سعر تكلفة الصنف (Item.CostPrice) وقت التسجيل
/// ولا يُعاد حسابها لاحقاً لو تغيّر سعر التكلفة مستقبلاً — فالسند يوثّق التكلفة التاريخية.
/// </summary>
public class StockWriteOff : BaseEntity
{
    /// <summary>رقم السند (يسلسل عبر NumberSequenceHelper ببادئة WO — مثل WO-20260908-0001).</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>مفتاح أجنبي إلى الصنف المُتلف.</summary>
    public Guid ItemId { get; set; }

    /// <summary>كائن التنقّل إلى الصنف.</summary>
    public Item? Item { get; set; }

    /// <summary>مفتاح أجنبي إلى المخزن الذي حدث فيه الإتلاف.</summary>
    public Guid WarehouseId { get; set; }

    /// <summary>كائن التنقّل إلى المخزن.</summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// الدُفعة المحددة المُتلفة (اختياري — يُملأ فقط لو كان الصنف يتتبّع دُفعات Item.TracksBatches).
    /// يُسمح بإتلاف دُفعة منتهية الصلاحية (هذا غالباً غرض السند).
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>كائن التنقّل إلى الدُفعة.</summary>
    public ItemBatch? Batch { get; set; }

    /// <summary>الكمية المُتلفة (موجبة دائماً في التسجيل؛ الحركة تُسجَّل سالبة).</summary>
    public decimal Quantity { get; set; }

    /// <summary>سبب الإتلاف من قائمة ثابتة (ما عدا "أخرى" يتطلب حقل نص حر).</summary>
    public StockWriteOffReason Reason { get; set; }

    /// <summary>تفسير نص حر إضافي — يُطلب فقط عند اختيار السبب "أخرى".</summary>
    public string? OtherReasonText { get; set; }

    /// <summary>تاريخ السند (تاريخ العمل، لا تاريخ الإنشاء).</summary>
    public DateTime Date { get; set; }

    /// <summary>سعر تكلفة الوحدة وقت الإتلاف — نسخة ثابتة من الصنف وقت التسجيل.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>ملاحظات اختيارية.</summary>
    public string? Notes { get; set; }

    /// <summary>معرّف مستخدم النظام (AspNetUsers.Id) الذي أنشأ السند.</summary>
    public string? CreatedByUserId { get; set; }

    /// <summary>القيد المحاسبي المرتبط بهذا السند (مدين مصروف هالك / دائن مخزون).</summary>
    public Guid JournalEntryId { get; set; }

    /// <summary>كائن التنقّل إلى القيد المحاسبي.</summary>
    public JournalEntry? JournalEntry { get; set; }
}