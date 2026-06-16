using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ShiftBlazor.Tagging;

/// <summary>
/// Configures the framework-provided tag management components
/// (<c>ShiftTagList</c>, <c>ShiftTagForm</c>, <c>ShiftTagPicker</c>).
/// Programmer registers via <c>services.AddShiftBlazorTagging(o =&gt; ...)</c>, then hosts the
/// components in their own routed pages (e.g. <c>@page "/tags"</c> → <c>&lt;ShiftTagList/&gt;</c>).
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
