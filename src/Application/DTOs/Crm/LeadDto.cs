namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for displaying a lead (عميل محتمل).
/// </summary>
public class LeadDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int Source { get; set; }
    public int Status { get; set; }
    public string? Notes { get; set; }
    public Guid? ConvertedCustomerId { get; set; }
    public string? ConvertedCustomerName { get; set; }
}
