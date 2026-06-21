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

    /// <summary>
    /// Same as <see cref="AddShiftBlazorTagging(IServiceCollection, Action{ShiftBlazorTaggingOptions})"/>,
    /// but also registers <typeparamref name="TActionTree"/> with client-side TypeAuth — so the tree
    /// that owns the tag node (typically set via <c>o.TypeAuthAction</c>) is known to the Blazor
    /// authorization layer without a separate <c>AddTypeAuth(x =&gt; x.AddActionTree&lt;TActionTree&gt;())</c>.
    /// Registering the same tree again elsewhere is harmless — <c>AddActionTree</c> is idempotent.
    /// </summary>
    public static IServiceCollection AddShiftBlazorTagging<TActionTree>(
        this IServiceCollection services,
        Action<ShiftBlazorTaggingOptions>? configure = null)
        where TActionTree : class
    {
        services.AddShiftBlazorTagging(configure);
        services.Configure<ShiftSoftware.TypeAuth.Blazor.TypeAuthBlazorOptions>(o => o.AddActionTree<TActionTree>());

        return services;
    }
}
