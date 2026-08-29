using ERPSystem.Application.DTOs.Inventory;

namespace ERPSystem.Application.Interfaces;

/// <summary>خدمة قراءة دفعات المخزون (تتبّع انتهاء الصلاحية).</summary>
public interface IStockBatchService
{
    /// <summary>كل الدفعات الحالية (الكمية المتبقية أكبر من صفر) مع بيانات الصنف والمخزن، مرتّبة بتاريخ الانتهاء.</summary>
    Task<List<StockBatchDto>> GetBatchesAsync();
}