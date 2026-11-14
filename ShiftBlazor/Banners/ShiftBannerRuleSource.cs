using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Banners;

/// <summary>
/// Loads the banner rules file once and hands the same rules to every <c>ShiftBanners</c> on the page.
/// A missing or malformed file, or any other error while loading it, shows no banners (and is logged) instead of
/// breaking the layout it sits in; the file is tried again on the next request.
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
    private Task<IReadOnlyList<ShiftBannerRule>?>? loading;

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

    /// <summary>
    /// The rules from the file, loaded on first use and cached for the lifetime of this service. A failed load is
    /// not cached: it returns no rules, and the next call tries the file again.
    /// </summary>
    public async Task<IReadOnlyList<ShiftBannerRule>> GetRulesAsync()
    {
        var current = loading ??= LoadAsync();
        var rules = await current;

        if (rules is null)
        {
            if (loading == current)
                loading = null;

            return Array.Empty<ShiftBannerRule>();
        }

        return rules;
    }

    // Null when the file could not be loaded. Any exception counts: a banner must never break the layout it sits in.
    private async Task<IReadOnlyList<ShiftBannerRule>?> LoadAsync()
    {
        Uri? url = null;

        try
        {
            // Relative to the app, not to HttpClient.BaseAddress, which usually points at the API.
            url = new Uri(new Uri(navigationManager.BaseUri), options.RulesPath.TrimStart('/'));

            var file = await httpClient.GetFromJsonAsync<ShiftBannerRuleFile>(url, JsonOptions);
            return WithoutNulls(file);
        }
        catch (Exception e)
        {
            logger?.LogWarning(e, "Banner rules could not be loaded from {Url}; no banners are shown.", url?.ToString() ?? options.RulesPath);
            return null;
        }
    }

    // JSON allows null anywhere, but the matcher and the caller's predicate expect none: drop null rules and list
    // entries, and treat a null list or message as empty.
    internal static IReadOnlyList<ShiftBannerRule> WithoutNulls(ShiftBannerRuleFile? file)
    {
        var rules = new List<ShiftBannerRule>();

        foreach (var rule in file?.Rules ?? new List<ShiftBannerRule>())
        {
            if (rule is null)
                continue;

            rule.Paths = rule.Paths?.Where(x => x is not null).ToList() ?? new();
            rule.Include = rule.Include?.Where(x => x is not null).ToList() ?? new();
            rule.Exclude = rule.Exclude?.Where(x => x is not null).ToList() ?? new();
            rule.Message ??= new();
            rules.Add(rule);
        }

        return rules;
    }
}
