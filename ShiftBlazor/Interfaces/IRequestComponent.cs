using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftBlazor.Utils;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShiftSoftware.ShiftBlazor.Interfaces;

public interface IRequest
{
    public string? Endpoint { get; }
    public string? BaseUrl { get; }
    public string? BaseUrlKey { get; }
}

public interface IEntityRequest : IRequest
{
    public object? Key { get; }
}

public interface IODataRequest : IRequest
{
    public string? EntitySet { get; }
}

public interface IRequestComponent : IRequest
{
    public HttpClient HttpClient { get; }
    public ShiftBlazorLocalizer Loc { get; }
    public SettingManager SettingManager { get; }

    public Func<HttpRequestMessage, ValueTask<bool>>? OnBeforeRequest { get; }
    public Func<HttpResponseMessage, ValueTask<bool>>? OnResponse { get; }
    public Func<Exception, ValueTask<bool>>? OnError { get; }

    public static string GetPath(IRequestComponent self)
    {
        string? url = self.BaseUrl;
        var config = self.SettingManager.Configuration;

        if (url is null && self.BaseUrlKey is not null)
            url = config.ExternalAddresses.TryGet(self.BaseUrlKey);

        url ??= config.BaseAddress;

        return url.AddUrlPath(self.Endpoint);
    }
}

public interface IEntityRequestComponent<T> : IEntityRequest, IRequestComponent
{
    public Func<ShiftEntityResponse<T>?, ValueTask<bool>>? OnResult { get; }
}

public interface IODataRequestComponent<T> : IODataRequest, IRequestComponent
{
    public Func<ODataDTO<T>?, ValueTask<bool>>? OnResult { get; }

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new LocalDateTimeOffsetJsonConverter() }
    };

    public static async Task<ODataDTO<T>?> GetFromJsonAsync(IODataRequestComponent<T> self, Uri url, CancellationToken token)
    {
        using var requestMessage = self.HttpClient.CreateRequestMessage(HttpMethod.Get, url);

        if (self.OnBeforeRequest != null && await self.OnBeforeRequest.Invoke(requestMessage))
            return null;

        using var res = await self.HttpClient.SendAsync(requestMessage, token);

        if (self.OnResponse != null && await self.OnResponse.Invoke(res))
            return null;

        if (!res.IsSuccessStatusCode)
        {
            throw new Exception(self.Loc["DataReadStatusError", (int)res.StatusCode]);
        }

        ODataDTO<T>? content;

        try
        {
            content = await res.Content.ReadFromJsonAsync<ODataDTO<T>>(SerializerOptions, token);
        }
        catch (JsonException e)
        {
            var body = res == null ? self.Loc["DataParseError"] : await res.Content.ReadAsStringAsync(token);
            throw new JsonException(body, e);
        }

        if (self.OnResult != null && await self.OnResult.Invoke(content))
            return null;

        if (content == null || content.Count == null)
        {
            throw new Exception(self.Loc["DataReadEmptyError"]);
        }

        return content;
    }
}
