using System.Net.Http.Json;
using System.Text.Json;

namespace System.Net.Http;

public static class HttpClientExtension
{

    public static HttpRequestMessage CreateIdempotencyRequest(this HttpClient httpClient, object? value, string url, Guid idempotencyToken, HttpMethod? method = null)
    {
        method ??= HttpMethod.Post;

        JsonContent content = JsonContent.Create(value, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var uri = new Uri(url);
        var request = CreateRequestMessage(httpClient, method, uri);
        request.Content = content;
        request.Headers.Add("Idempotency-Key", idempotencyToken.ToString());

        return request;

    }

    public static HttpRequestMessage CreateRequestMessage(this HttpClient httpClient, HttpMethod method, Uri? uri)
    {
        return new HttpRequestMessage(method, uri)
        {
            Version = httpClient.DefaultRequestVersion,
            VersionPolicy = httpClient.DefaultVersionPolicy,
        };
    }
}
