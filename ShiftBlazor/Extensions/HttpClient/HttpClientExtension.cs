using System.Net.Http.Json;
using System.Text.Json;

namespace System.Net.Http;

public static class HttpClientExtension
{

    public static HttpRequestMessage CreateIdempotencyRequest(this HttpClient httpClient, object? value, string url, Guid idempotencyToken, HttpMethod? method = null)
    {
        method ??= HttpMethod.Post;

        JsonContent content = JsonContent.Create(value, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return new HttpRequestMessage(method, url)
        {
            Version = httpClient.DefaultRequestVersion,
            VersionPolicy = httpClient.DefaultVersionPolicy,
            Content = content,
            Headers =
            {
                { "Idempotency-Key", idempotencyToken.ToString() },
            },
        };
    }
}
