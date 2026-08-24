using System.Text.Json;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Singleton deployment settings — one row per deployment (fixed Id).
/// Controlled exclusively by the vendor owner console, not by tenant admins.
/// </summary>
public class SystemSettings
{
    /// <summary>Fixed singleton Id — all reads/upserts use this value.</summary>
    public static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000099");

    public Guid Id { get; set; } = SingletonId;

    /// <summary>Client logo URL/path (e.g. /uploads/branding/logo.png)</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Override app display name shown in header/PageTitle</summary>
    public string? AppDisplayName { get; set; }

    /// <summary>Optional theme accent color hex (e.g. #6D5BD0)</summary>
    public string? PrimaryColorHex { get; set; }

    public string LicensedToClientName { get; set; } = "Default Client";
    public DateTime? LicenseExpiryDate { get; set; }
    public bool IsDeploymentActive { get; set; } = true;
    public string? SupportContactInfo { get; set; }

    /// <summary>JSON serialized Dictionary&lt;string,bool&gt; mapping module keys to enabled state.</summary>
    public string FeatureFlagsJson { get; set; } = "";

    // ── Part D: Subscription billing cycle ──
    public SubscriptionCycle SubscriptionCycle { get; set; } = SubscriptionCycle.Annual;
    public DateTime SubscriptionStartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime NextRenewalDate { get; set; } = DateTime.UtcNow.Date.AddYears(1);
    public DateTime? LastRenewedAt { get; set; }

    // ── Part E: Backup destination (credentials in secrets, not DB) ──
    public BackupDestinationType BackupDestinationType { get; set; } = BackupDestinationType.None;
    public string? BackupDestination { get; set; }

    // ── رمز العملة الموحد للنظام (عملة واحدة فقط لكل نشر) ──
    // لماذا هنا في SystemSettings وليس جدول منفصل؟
    // لأنها قيمة عامة واحدة مثل AppDisplayName/LogoUrl، لا تحتاج جدولاً مستقلاً.
    // ولماذا string وليس مفتاحاً أجنبياً؟ لأنه لا يوجد تعدد عملات — بسيطة عمداً.
    // تغييرها عملية عرض فقط، لا تعيد حساب أي مبلغ مخزن (لا تحويل).
    public string CurrencyCode { get; set; } = "JOD";

    // ── Part K: Trial / demo mode ──
    public bool IsTrialMode { get; set; } = false;
    public DateTime? TrialStartedAt { get; set; }
    public DateTime? TrialExpiresAt { get; set; }

    // ── Part I: Update log / health history (not columns, separate tables) ──

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static DateTime ComputeNextRenewal(DateTime start, SubscriptionCycle cycle) => cycle switch
    {
        SubscriptionCycle.Monthly => start.AddMonths(1),
        SubscriptionCycle.Quarterly => start.AddMonths(3),
        SubscriptionCycle.SemiAnnual => start.AddMonths(6),
        SubscriptionCycle.Annual => start.AddYears(1),
        _ => start.AddYears(1)
    };

    /// <summary>
    /// All module keys in the system — must stay in sync with ModuleRoles in Program.cs
    /// </summary>
    public static readonly string[] AllModuleKeys =
    {
        "Sales", "Purchases", "Inventory", "Accounting", "Expenses", "Crm", "Hr", "Reports", "Pos", "Permissions", "JoFotara"
    };

    /// <summary>Default: all modules enabled.</summary>
    public static string DefaultFeatureFlagsJson()
    {
        var dict = AllModuleKeys.ToDictionary(k => k, _ => true);
        return JsonSerializer.Serialize(dict);
    }

    public Dictionary<string, bool> GetFeatureFlags()
    {
        if (string.IsNullOrWhiteSpace(FeatureFlagsJson))
            return AllModuleKeys.ToDictionary(k => k, _ => true);
        try
        {
            var d = JsonSerializer.Deserialize<Dictionary<string, bool>>(FeatureFlagsJson);
            if (d == null) return AllModuleKeys.ToDictionary(k => k, _ => true);
            // Ensure any missing new keys default to true
            foreach (var k in AllModuleKeys)
                if (!d.ContainsKey(k)) d[k] = true;
            return d;
        }
        catch
        {
            return AllModuleKeys.ToDictionary(k => k, _ => true);
        }
    }

    public void SetFeatureFlags(Dictionary<string, bool> flags)
    {
        FeatureFlagsJson = JsonSerializer.Serialize(flags);
    }
}
