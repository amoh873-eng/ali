namespace ERPSystem.Application.DTOs.Warehouses;

/// <summary>
/// Data Transfer Object for displaying a warehouse.
/// </summary>
public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public bool IsDefault { get; set; }
}