using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Banners;

/// <summary>
/// Loads the banner rules file once and hands the same rules to every <c>ShiftBanners</c> on the page.
/// A missing or malformed file shows no banners (and is logged) instead of breaking the layout it sits in.
/// </summary>
public class ShiftBannerRuleSource
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly HttpClient httpClient;
    private readonly NavigationManager navigationManager;
    private readonly ShiftBannerOptions options;
    private readonly ILogger<ShiftBannerRuleSource>? logger;
    private Task<IReadOnlyList<ShiftBannerRule>>? loading;

    public ShiftBannerRuleSource(
        HttpClient httpClient,
        NavigationManager navigationManager,
        IOptions<ShiftBannerOptions> options,
        ILogger<ShiftBannerRuleSource>? logger = null)
    {
        this.httpClient = httpClient;
        this.navigationManager = navigationManager;
        this.options = options.Value;
        this.logger = logger;
    }

    /// <summary>The rules from the file, loaded on first use and cached for the lifetime of this service.</summary>
    public Task<IReadOnlyList<ShiftBannerRule>> GetRulesAsync() => loading ??= LoadAsync();

    private async Task<IReadOnlyList<ShiftBannerRule>> LoadAsync()
    {
        // Relative to the app, not to HttpClient.BaseAddress, which usually points at the API.
        var url = new Uri(new Uri(navigationManager.BaseUri), options.RulesPath.TrimStart('/'));

        try
        {
            var file = await httpClient.GetFromJsonAsync<ShiftBannerRuleFile>(url, JsonOptions);
            return file?.Rules ?? new List<ShiftBannerRule>();
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
        {
            logger?.LogWarning(e, "Banner rules could not be loaded from {Url}; no banners are shown.", url);
            return new List<ShiftBannerRule>();
        }
    }
}
