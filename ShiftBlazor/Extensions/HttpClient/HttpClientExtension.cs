using System.Net.Http.Json;
using System.Text.Json;

namespace System.Net.Http;

public static class HttpClientExtension
{
    public static HttpRequestMessage CreatePostRequest(this HttpClient httpClient, object? value, string url, Guid? idempotencyToken = null)
    {
        JsonContent content = JsonContent.Create(value, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var uri = new Uri(url);

        var request = CreateRequestMessage(httpClient, HttpMethod.Post, uri);

        request.Content = content;

        if (idempotencyToken is not null)
            request.Headers.Add("Idempotency-Key", idempotencyToken.ToString());

        return request;
    }

    public static HttpRequestMessage CreatePutRequest(this HttpClient httpClient, object? value, string url)
    {
        JsonContent content = JsonContent.Create(value, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var uri = new Uri(url);

        var request = CreateRequestMessage(httpClient, HttpMethod.Put, uri);

        request.Content = content;

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
