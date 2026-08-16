using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Application.DTOs.Crm;

/// <summary>
/// DTO for updating an existing lead.
/// </summary>
public class UpdateLeadDto
{
    [Required]
    public Guid Id { get; set; }

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

    [Range(1, 6)]
    public int Source { get; set; }

    [Range(1, 5)]
    public int Status { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
