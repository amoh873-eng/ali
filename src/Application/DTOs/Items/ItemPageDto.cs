namespace ERPSystem.Application.DTOs.Items;

/// <summary>
/// صفحة من الأصناف + العدد الإجمالي (بدون ترقيم).
/// يُستخدم في جداول الخادم ServerData (مثل شاشة ملصقات الباركود)
/// بدلاً من تحميل كل الأصناف في الذاكرة (اللود الضخم الذي كان يجمّد الجهاز).
/// </summary>
public class ItemPageDto
{
    public List<ItemDto> Items { get; set; } = new();
    public int Total { get; set; }
}