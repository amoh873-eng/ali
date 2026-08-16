using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for creating a new lead.
/// </summary>
public class CreateLeadDto
{
    [Required]
    [StringLength(200)]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameEn { get; set; }

    [StringLength(200)]
    public string? ContactPerson { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// 1 = Website, 2 = Phone, 3 = Referral, 4 = Walk-in, 5 = Social media, 6 = Other.
    /// </summary>
    [Range(1, 6)]
    public int Source { get; set; } = 1;

    [StringLength(1000)]
    public string? Notes { get; set; }
}
