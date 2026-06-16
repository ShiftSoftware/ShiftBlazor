using Microsoft.Extensions.DependencyInjection;
using System;

namespace ShiftSoftware.ShiftBlazor.Tagging;

public static class ShiftBlazorTaggingServiceCollectionExtensions
{
    /// <summary>
    /// Configures the tag management components (<see cref="Components.ShiftTagList"/>,
    /// <see cref="Components.ShiftTagForm"/>, <see cref="Components.ShiftTagPicker"/>): entity set,
    /// API base URL, titles, and the TypeAuth action that gates them.
    ///
    /// These are plain components, not routed pages — the programmer creates the pages and drops
    /// the components in:
    /// <code>
    /// @page "/tags"
    /// &lt;ShiftTagList /&gt;
    ///
    /// @page "/tags/{Key?}"
    /// &lt;ShiftTagForm Key="@Key" /&gt;
    /// </code>
    /// so there is no router/assembly wiring to do. Call this once to supply the shared config.
    /// </summary>
    public static IServiceCollection AddShiftBlazorTagging(
        this IServiceCollection services,
        Action<ShiftBlazorTaggingOptions>? configure = null)
    {
        services.Configure<ShiftBlazorTaggingOptions>(o => configure?.Invoke(o));
        return services;
    }
}
