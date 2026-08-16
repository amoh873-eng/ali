namespace ERPSystem.Application.DTOs.Positions;

/// <summary>
/// DTO for displaying a job position.
/// </summary>
public class PositionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public Guid DepartmentId { get; set; }
    public string? DepartmentNameAr { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
}
