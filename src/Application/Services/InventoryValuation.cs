namespace ERPSystem.Application.Services;

/// <summary>
/// Inventory valuation helpers (تقييم المخزون).
///
/// المتوسط المرجح (Weighted Average):
/// عند كل وارد تُعاد حساب تكلفة الوحدة على أساس متوسط القيمة الكلية:
///   newCost = (oldQty × oldCost + receivedQty × receivedCost) / (oldQty + receivedQty)
/// هذا يوزّع فروق التكلفة بين المشتريات المختلفة بشكل متساوٍ، وهو الأنسب للبداية
/// لسهولته وقبوله محاسبياً مقارنة بـ FIFO / LIFO.
/// </summary>
public static class InventoryValuation
{
    /// <summary>
    /// Computes the new weighted-average unit cost after an inbound movement.
    /// </summary>
    public static decimal ComputeWeightedAverage(
        decimal oldQuantity, decimal oldCost, decimal receivedQuantity, decimal receivedUnitCost)
    {
        var totalQuantity = oldQuantity + receivedQuantity;
        if (totalQuantity <= 0)
            return oldCost;

        var oldValue = oldQuantity * oldCost;
        var receivedValue = receivedQuantity * receivedUnitCost;
        return Math.Round((oldValue + receivedValue) / totalQuantity, 2);
    }
}