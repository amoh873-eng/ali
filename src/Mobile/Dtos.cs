namespace ERPSystem.Mobile;

/// <summary>نماذج JSON من واجهة الـ API (للمرحلة 1).</summary>
public record LoginResponseDto(string Token, string UserEmail, string RolesCsv) { }

public record ItemDto(string Id, string Code, string NameAr, string? NameEn, string SalePrice, string CurrentStock) { }

public record CustomerDto(string Id, string Code, string NameAr, string? Phone) { }

public record WarehouseDto(string Id, string Code, string NameAr) { }

public record InvoiceDto(string Id, string InvoiceNumber, string TotalAmount, string Note) { }

/// <summary>سطر صنف داخل فاتورة المبيعات.</summary>
public record InvoiceLineDto(string ItemId, string Quantity, string UnitPrice) { }

/// <summary>سطر فاتورة قابل للرد (من بحث المردود).</summary>
public record ReturnLookupLineDto(string ItemId, string Code, string NameAr, string Quantity, string UnitPrice, string Remaining) { }

/// <summary>نتيجة البحث عن فاتورة أصلية للمردود.</summary>
public record ReturnLookupDto(string InvoiceId, string InvoiceNumber, string CustomerId, string CustomerName, string WarehouseId, string WarehouseName, string InvoiceDate, string TotalAmount, List<ReturnLookupLineDto> Lines) { }

/// <summary>سطر مردود يُرسل للخادم.</summary>
public record CreateReturnLineDto(string ItemId, string Quantity) { }

/// <summary>نتيجة إنشاء المردود.</summary>
public record ReturnDto(string Id, string ReturnNumber, string InvoiceNumber, string TotalAmount) { }

/// <summary>عنصر طابور المزامنة المحلي.</summary>
public record OutboxItem(string ClientKey, string Payload, string Status) { }