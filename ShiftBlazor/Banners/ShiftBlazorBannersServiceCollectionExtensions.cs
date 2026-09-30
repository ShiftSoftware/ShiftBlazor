using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace ShiftSoftware.ShiftBlazor.Banners;

public static class ShiftBlazorBannersServiceCollectionExtensions
{
    /// <summary>
    /// Enables <see cref="Components.ShiftBanners"/>: warning banners driven by a rules file in <c>wwwroot</c>
    /// (<see cref="ShiftBannerOptions.RulesPath"/>, default <c>banners.json</c>). Place the component once, usually in
    /// the main layout, and pass it the predicate that decides the rules' include/exclude conditions:
    /// <code>
    /// builder.Services.AddShiftBlazorBanners();
    ///
    /// &lt;ShiftBanners IsConditionMet="@(condition =&gt; condition == "role:accountant" &amp;&amp; isAccountant)" /&gt;
    /// </code>
    /// </summary>
    public static IServiceCollection AddShiftBlazorBanners(
        this IServiceCollection services,
        Action<ShiftBannerOptions>? configure = null)
    {
        services.Configure<ShiftBannerOptions>(o => configure?.Invoke(o));
        services.TryAddScoped<ShiftBannerRuleSource>();
        return services;
    }
}
