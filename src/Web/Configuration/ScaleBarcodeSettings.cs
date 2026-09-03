using System.Text.Json.Serialization;

namespace ERPSystem.Web.Configuration;

/// <summary>
/// Non-invasive scale-barcode interceptor settings (bound from appsettings.json via IOptions).
/// This module is a wrapper ONLY: it does NOT change the DB schema, existing pages,
/// or the backend POS logic. It captures hardware-scanner input on the POS page,
/// parses the EAN-13 scale barcode, then re-injects a barcode string that the EXISTING
/// Pos.razor search pipeline (BarcodeParser + HandleSearchKeyDown) already understands.
/// </summary>
public sealed class ScaleBarcodeSettings
{
    public const string SectionName = "ScaleBarcode";

    /// <summary>Master switch — set false to disable the interceptor entirely (scanner behaves as a normal keyboard).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Allowed scale prefixes (first two digits of the EAN-13). Standard variable-weight scales use 20-29.</summary>
    public string[] Prefixes { get; set; } = { "21", "22" };

    /// <summary>"weight" or "price" — what digits 8-12 represent on this deployment's scales.</summary>
    public string Mode { get; set; } = "weight";

    /// <summary>Division factor applied to digits 8-12 (e.g. 1000 converts grams to kilograms, 100 → price with 2 decimals).</summary>
    public int Divisor { get; set; } = 1000;

    /// <summary>Max milliseconds between keystrokes to be considered a scanner burst (human typing is slower and passes through untouched).</summary>
    public double ScanSpeedMs { get; set; } = 80;

    /// <summary>Only capture input while the current URL contains this path (page-path whitelist).</summary>
    public string PagePath { get; set; } = "/pos";

    /// <summary>CSS selector of the POS product-code search input the interceptor fills + presses Enter on.</summary>
    public string SearchSelector { get; set; } = ".pos-search input";

    /// <summary>Seconds the parsed DB rule is cached to avoid a DB hit on every scan (0 disables caching).</summary>
    public int RuleCacheSeconds { get; set; } = 60;
}

/// <summary>
/// Structured payload passed from scaleBarcode.js back into the Blazor component
/// through [JSInvokable] ReceiveScaleBarcode.
/// </summary>
public sealed class ScaleBarcodePayload
{
    /// <summary>Product SKU / item code — digits 3-7 of the EAN-13 scale barcode.</summary>
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Weight or total price (digits 8-12 divided by Divisor).</summary>
    [JsonPropertyName("value")]
    public decimal Value { get; set; }

    /// <summary>"weight" | "price" (mirrors the configured Mode).</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>The full 13-digit code as scanned (fallback when no DB rule is configured).</summary>
    [JsonPropertyName("raw")]
    public string Raw { get; set; } = string.Empty;
}