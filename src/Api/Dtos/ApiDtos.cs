namespace ERPSystem.Api.Dtos;

public record LoginRequest(string Email, string Password) { }

public record ApiUserDto(string Id, string? Email, string? Name, IEnumerable<string> Roles) { }

public record LoginResponse(string Token, string TokenType, int ExpiresIn, ApiUserDto User) { }

public record ApiError(string Error) { }

public record ItemCard(string Id, string Code, string NameAr, string? NameEn, decimal? SalePrice, decimal? CostPrice, decimal CurrentStock, string? Barcode, string? UnitNameAr, string? ImageUrl) { }

public record CustomerCard(string Id, string Code, string NameAr, string? NameEn, string? Phone, decimal CurrentBalance) { }

public record WarehouseCard(string Id, string Code, string NameAr, string? NameEn, bool IsActive) { }

public record InvoiceLineCard(Guid ItemId, string? ItemCode, string? ItemNameAr, decimal Quantity, decimal UnitPrice, decimal LineTotal) { }

public record InvoiceCard(Guid Id, string InvoiceNumber, DateTime InvoiceDate, string? CustomerName,
                          decimal SubTotal, decimal DiscountAmount, decimal TaxAmount, decimal TotalAmount,
                          string? Note, bool IsPos, List<InvoiceLineCard> Lines) { }

public record CreateInvoiceRequest(Guid? CustomerId, Guid WarehouseId, int? PaymentMethod,
                                   decimal? DiscountPercentage, decimal? TaxRate, string? Note, IEnumerable<CreateInvoiceLineRequest> Lines) { }

public record CreateInvoiceLineRequest(Guid ItemId, decimal Quantity, decimal UnitPrice) { }