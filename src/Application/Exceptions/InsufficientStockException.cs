namespace ERPSystem.Application.Exceptions;

/// <summary>
/// يُرمى عندما يكون الرصيد المتاح (مجموع حركات المخزون للصنف في المخزن) أقل من الكمية المطلوبة.
/// يحمل معرّف واسم الصنف ليتمكن العميل (مثل شاشة نقطة البيع) من عرض الاسم وتمييز الصنف بصرياً.
/// </summary>
public sealed class InsufficientStockException : InvalidOperationException
{
    public Guid ItemId { get; }
    public string ItemName { get; }
    public decimal Available { get; }
    public decimal Requested { get; }

    public InsufficientStockException(Guid itemId, string itemName, decimal available, decimal requested)
        : base($"الرصيد غير كافٍ للصنف «{itemName}». المتاح: {available:N0}، المطلوب: {requested:N0}.")
    {
        ItemId = itemId;
        ItemName = itemName;
        Available = available;
        Requested = requested;
    }
}