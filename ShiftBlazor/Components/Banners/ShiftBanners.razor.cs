using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftBlazor.Banners;
using ShiftSoftware.ShiftBlazor.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Components;

/// <summary>
/// Shows the warning banners whose rules match the current page. Rules come from the file configured by
/// <c>services.AddShiftBlazorBanners(...)</c>; the banners update on every navigation.
/// <code>
/// &lt;ShiftBanners IsConditionMet="IsConditionMet" /&gt;
/// </code>
/// </summary>
public partial class ShiftBanners : ComponentBase, IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private SettingManager SettingManager { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    /// <summary>
    /// Decides the rules' <c>include</c> / <c>exclude</c> condition names, e.g. <c>"role:accountant"</c>.
    /// Without it no condition is met: rules with <c>include</c> stay hidden and nothing is excluded.
    /// </summary>
    [Parameter] public Func<string, bool>? IsConditionMet { get; set; }

    /// <summary>Extra CSS classes for each banner.</summary>
    [Parameter] public string? Class { get; set; }

    private IReadOnlyList<ShiftBannerRule> rules = Array.Empty<ShiftBannerRule>();
    private IReadOnlyList<(ShiftBannerRule Rule, string Text)> banners = Array.Empty<(ShiftBannerRule, string)>();

    protected override async Task OnInitializedAsync()
    {
        var source = ServiceProvider.GetService<ShiftBannerRuleSource>()
            ?? throw new InvalidOperationException(
                $"{nameof(ShiftBanners)} needs its services: call builder.Services.{nameof(ShiftBlazorBannersServiceCollectionExtensions.AddShiftBlazorBanners)}() at startup.");

        NavigationManager.LocationChanged += OnLocationChanged;
        rules = await source.GetRulesAsync();
        Refresh();
    }

    // A new predicate (for example after sign-in) can change which rules apply.
    protected override void OnParametersSet() => Refresh();

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        Refresh();
        InvokeAsync(StateHasChanged);
    }

    private void Refresh()
    {
        var path = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        banners = ShiftBannerMatcher.GetActiveBanners(rules, path, IsConditionMet, SettingManager.GetCulture());
    }

    public void Dispose() => NavigationManager.LocationChanged -= OnLocationChanged;
}
