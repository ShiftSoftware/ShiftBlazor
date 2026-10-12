using System.Net.Http.Json;
using ShiftSoftware.ShiftBlazor.Interfaces;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftBlazor.Components.Pii;

/// <summary>The one-field reveal call shared by <c>ShiftPiiField</c> and <c>ShiftPiiText</c>.</summary>
internal static class PiiRevealRequest
{
    /// <summary>
    /// Explicit base URL, then the explicit config key, then the owning request's (form or list),
    /// then the configured base address.
    /// </summary>
    public static string BuildUrl(SettingManager settings, string? baseUrl, string? baseUrlKey, IRequest? owner,
        string? endpoint, string recordKey, string member)
    {
        var configuration = settings.Configuration;
        var resolved = baseUrl
            ?? (baseUrlKey is not null ? configuration.ExternalAddresses.TryGet(baseUrlKey) : null)
            ?? owner?.BaseUrl
            ?? (owner?.BaseUrlKey is not null ? configuration.ExternalAddresses.TryGet(owner.BaseUrlKey) : null)
            ?? configuration.BaseAddress;
        return resolved.AddUrlPath(
            endpoint, Uri.EscapeDataString(recordKey), "pii", Uri.EscapeDataString(member), "reveal");
    }

    /// <summary><c>Value</c> is set only when the reveal succeeded; <c>Forbidden</c> tells a refused grant apart.</summary>
    public sealed record Result(bool Succeeded, string? Value, bool Forbidden);

    public static async Task<Result> SendAsync(HttpClient http, string url)
    {
        try
        {
            using var response = await http.PostAsync(url, null);
            if (!response.IsSuccessStatusCode)
                return new(false, null, response.StatusCode == System.Net.HttpStatusCode.Forbidden);
            return new(true, (await response.Content.ReadFromJsonAsync<PiiRevealDTO>())?.Value, false);
        }
        catch (HttpRequestException)
        {
            return new(false, null, false);
        }
    }
}
