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

    // Above this many items the value array is materialized in batches with a yield to the browser
    // between batches; below it a single pass is cheaper (no envelope re-walk, no yield overhead).
    private const int DeserializeBatchThreshold = 200;
    private const int DeserializeBatchSize = 100;

    /// <summary>
    /// Deserializes the OData envelope without blocking the UI thread for the whole payload.
    /// WASM is single-threaded: binding a large response in one synchronous chunk freezes rendering
    /// and input for the entire parse (seconds at hundreds of rows on the interpreted runtime).
    /// Instead, the envelope is tokenized once (structural scan only — cheap), then items are bound
    /// in batches of <see cref="DeserializeBatchSize"/> with a real macrotask yield (Task.Delay)
    /// between batches so the browser can paint and handle input throughout. Total CPU is slightly
    /// higher than a single pass; perceived responsiveness is the point.
    /// </summary>
    private static async Task<ODataDTO<T>?> DeserializeODataInBatchesAsync(byte[] bytes, CancellationToken token)
    {
        using var doc = JsonDocument.Parse(bytes);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            return JsonSerializer.Deserialize<ODataDTO<T>>(bytes, SerializerOptions);

        JsonElement valueElement = default;
        var hasValue = false;
        long? count = null;

        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, "value", StringComparison.OrdinalIgnoreCase))
            {
                valueElement = prop.Value;
                hasValue = true;
            }
            else if (prop.Name.EndsWith("count", StringComparison.OrdinalIgnoreCase))
            {
                // Plain "count" (the framework's shape) or OData's "@odata.count".
                if (prop.Value.ValueKind == JsonValueKind.Number)
                    count = prop.Value.GetInt64();
                else if (prop.Value.ValueKind == JsonValueKind.String && long.TryParse(prop.Value.GetString(), out var parsed))
                    count = parsed;
            }
        }

        if (!hasValue || valueElement.ValueKind != JsonValueKind.Array)
            return JsonSerializer.Deserialize<ODataDTO<T>>(bytes, SerializerOptions); // unexpected shape — classic path

        var itemCount = valueElement.GetArrayLength();
        var items = new List<T>(itemCount);
        var sinceYield = 0;

        foreach (var element in valueElement.EnumerateArray())
        {
            items.Add(element.Deserialize<T>(SerializerOptions)!);

            if (itemCount > DeserializeBatchThreshold && ++sinceYield >= DeserializeBatchSize)
            {
                sinceYield = 0;
                // Task.Delay (not Task.Yield): a guaranteed macrotask, so the browser actually gets
                // a turn to paint/process input between batches.
                await Task.Delay(1, token);
            }
        }

        return new ODataDTO<T> { Count = count, Value = items };
    }

    public static async Task<ODataDTO<T>?> GetFromJsonAsync(IODataRequestComponent<T> self, Uri url, CancellationToken token)
    {
        using var requestMessage = self.HttpClient.CreateRequestMessage(HttpMethod.Get, url);

        if (self.OnBeforeRequest != null && !(await self.OnBeforeRequest.Invoke(requestMessage)))
            return null;

        var perfTimer = ShiftListPerfProbe.LogTimings ? System.Diagnostics.Stopwatch.StartNew() : null;

        using var res = await self.HttpClient.SendAsync(requestMessage, token);

        var networkMs = perfTimer?.ElapsedMilliseconds;
        perfTimer?.Restart();

        if (self.OnResponse != null && !(await self.OnResponse.Invoke(res)))
            return null;

        if (!res.IsSuccessStatusCode)
        {
            //Check if shift entity error
            var shiftEntityException = await res.Content.ReadFromJsonAsync<ShiftEntityException>(SerializerOptions, token);

            if (shiftEntityException != null)
                throw shiftEntityException;

            throw new Exception(self.Loc["DataReadStatusError", (int)res.StatusCode]);
        }

        ODataDTO<T>? content;
        var bytes = await res.Content.ReadAsByteArrayAsync(token);

        try
        {
            content = await DeserializeODataInBatchesAsync(bytes, token);
        }
        catch (JsonException e)
        {
            throw new JsonException(System.Text.Encoding.UTF8.GetString(bytes), e);
        }

        if (perfTimer is not null)
            Console.WriteLine($"[ShiftListPerf] {typeof(T).Name}: network={networkMs}ms deserialize={perfTimer.ElapsedMilliseconds}ms items={content?.Value?.Count} bytes={bytes.Length}");

        if (self.OnResult != null && !(await self.OnResult.Invoke(content)))
            return null;

        if (content == null || content.Count == null)
        {
            throw new Exception(self.Loc["DataReadEmptyError"]);
        }

        return content;
    }
}
