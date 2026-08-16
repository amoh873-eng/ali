namespace ERPSystem.Application.DTOs.Units;

/// <summary>
/// Data Transfer Object for displaying a unit of measure.
/// </summary>
public class UnitDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
}