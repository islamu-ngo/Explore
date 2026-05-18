using System.Net.Http;
using System.Net.Http.Json;

namespace Explore.Blazor.Client.Services.Http;

/// <summary>
/// BFF HTTP facade for browser-side same-origin calls that rely on the shared WASM handler pipeline.
/// BrowserCredentialsMessageHandler adds credentials and antiforgery headers for mutating requests.
/// </summary>
public interface IBffClient
{
    Task<T?> GetAsync<T>(string path, CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync<T>(string path, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync(string path, CancellationToken ct = default);
    Task<HttpResponseMessage> PutAsync<T>(string path, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> PatchAsync<T>(string path, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct = default);
    Task<HttpResponseMessage> PostMultipartAsync(string path, MultipartFormDataContent content, CancellationToken ct = default);
    Task<TResponse?> SendAsync<TBody, TResponse>(HttpMethod method, string path, TBody body, CancellationToken ct = default);
}

public sealed class BffClient : IBffClient
{
    private readonly HttpClient _http;

    public BffClient(HttpClient http)
    {
        _http = http;
    }

    // ── GET ──────────────────────────────────────────────────────────────

    public Task<T?> GetAsync<T>(string path, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<T>(path, ct);

    // ── Mutating verbs ───────────────────────────────────────────────────

    public Task<HttpResponseMessage> PostAsync<T>(string path, T body, CancellationToken ct = default) =>
        SendMutatingAsync(HttpMethod.Post, path, JsonContent.Create(body), ct);

    public Task<HttpResponseMessage> PostAsync(string path, CancellationToken ct = default) =>
        SendMutatingAsync(HttpMethod.Post, path, content: null, ct);

    public Task<HttpResponseMessage> PutAsync<T>(string path, T body, CancellationToken ct = default) =>
        SendMutatingAsync(HttpMethod.Put, path, JsonContent.Create(body), ct);

    public Task<HttpResponseMessage> PatchAsync<T>(string path, T body, CancellationToken ct = default) =>
        SendMutatingAsync(HttpMethod.Patch, path, JsonContent.Create(body), ct);

    public Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct = default) =>
        SendMutatingAsync(HttpMethod.Delete, path, content: null, ct);

    public Task<HttpResponseMessage> PostMultipartAsync(
        string path, MultipartFormDataContent content, CancellationToken ct = default) =>
        SendMutatingAsync(HttpMethod.Post, path, content, ct);

    // ── Typed response helper ────────────────────────────────────────────

    /// <summary>
    /// Sends a mutating request and deserializes the JSON response body.
    /// Returns <c>default</c> when the response is not a success status code.
    /// </summary>
    public async Task<TResponse?> SendAsync<TBody, TResponse>(
        HttpMethod method, string path, TBody body, CancellationToken ct = default)
    {
        using var response = await SendMutatingAsync(method, path, JsonContent.Create(body), ct);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(ct);
    }

    // ── Core send ────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> SendMutatingAsync(
        HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, path) { Content = content };
        return await _http.SendAsync(req, ct);
    }
}
