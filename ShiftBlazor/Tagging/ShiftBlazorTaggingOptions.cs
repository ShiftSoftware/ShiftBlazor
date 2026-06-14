using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ShiftBlazor.Tagging;

/// <summary>
/// Configures the framework-provided tag management pages (<c>TagListPage</c>,
/// <c>TagFormPage</c>) hosted in <c>ShiftSoftware.ShiftBlazor.Pages.Tagging</c>.
/// Programmer registers via <c>services.AddShiftBlazorTagging(o =&gt; ...)</c> and
/// adds the ShiftBlazor assembly to <c>Router.AdditionalAssemblies</c> so the
/// <c>@page</c> routes become active.
///
/// To use custom pages instead, simply do NOT register this and do NOT add the
/// ShiftBlazor assembly to <c>AdditionalAssemblies</c> — author your own routed
/// pages composing <c>ShiftList&lt;TagListDTO&gt;</c> and
/// <c>ShiftEntityForm&lt;TagDTO&gt;</c> directly.
/// </summary>
public class ShiftBlazorTaggingOptions
{
    /// <summary>OData entity set name for tags. Default <c>tags</c>.</summary>
    public string EntitySet { get; set; } = "tags";

    /// <summary>Override the OData endpoint path (rare). Falls back to <see cref="EntitySet"/>.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Absolute base URL for the tagging API (overrides SettingManager).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Lookup key for resolving the API base URL from settings.</summary>
    public string? BaseUrlKey { get; set; }

    /// <summary>Page title for the list page. Default <c>"Tags"</c>.</summary>
    public string ListTitle { get; set; } = "Tags";

    /// <summary>Page title for the form page. Default <c>"Tag"</c>.</summary>
    public string FormTitle { get; set; } = "Tag";

    /// <summary>
    /// The action-tree node whose Read/Write/Delete access levels gate the pages.
    /// Should match the same node passed to the backend's <c>AddShiftTagging</c>.
    /// </summary>
    public ReadWriteDeleteAction? TypeAuthAction { get; set; }
}
