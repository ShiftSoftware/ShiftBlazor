namespace ShiftSoftware.ShiftBlazor.Banners;

/// <summary>
/// Configures <c>ShiftBanners</c>. Register with <c>services.AddShiftBlazorBanners(o =&gt; ...)</c>.
/// </summary>
public class ShiftBannerOptions
{
    /// <summary>
    /// Where the rules file is, relative to the app's base URI (the <c>wwwroot</c> folder of a WebAssembly app).
    /// Default <c>banners.json</c>. The file is read once per app load (see <see cref="ShiftBannerRuleFile"/> for
    /// its shape).
    /// </summary>
    public string RulesPath { get; set; } = "banners.json";
}
