using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftBlazor.Extensions;
using System;

namespace ShiftSoftware.ShiftBlazor.Tagging;

public static class ShiftBlazorTaggingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the framework's built-in tag management pages (<c>TagListPage</c> at
    /// <c>/tags</c> and <c>TagFormPage</c> at <c>/tags/{Key?}</c>) and exposes their
    /// configuration surface.
    ///
    /// Internally adds the ShiftBlazor assembly to <see cref="AppStartupOptions.AdditionalAssemblies"/>
    /// so <c>DefaultApp</c>'s Router picks up the routes automatically — the programmer
    /// does not need to touch <c>App.razor</c> or set <c>Router.AdditionalAssemblies</c> manually.
    ///
    /// To use custom pages instead, simply skip this call and author your own routed
    /// pages composing <c>ShiftList&lt;TagListDTO&gt;</c> + <c>ShiftEntityForm&lt;TagDTO&gt;</c>.
    /// </summary>
    public static IServiceCollection AddShiftBlazorTagging(
        this IServiceCollection services,
        Action<ShiftBlazorTaggingOptions>? configure = null)
    {
        services.Configure<ShiftBlazorTaggingOptions>(o => configure?.Invoke(o));

        // Auto-register the ShiftBlazor assembly so DefaultApp / Router picks up
        // TagListPage and TagFormPage without the programmer touching App.razor.
        services.Configure<AppStartupOptions>(o => o.AddAssembly(typeof(ShiftBlazorTaggingOptions).Assembly));

        return services;
    }
}
