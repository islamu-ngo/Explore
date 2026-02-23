using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// BFF HTTP facade that adds XSRF tokens to mutating requests.
/// All API calls from WASM route through the BFF reverse proxy (YARP).
/// </summary>
public sealed class BffClient
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private IJSObjectReference? _mod;

    public BffClient(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    private async Task<string> GetXsrfAsync()
    {
        _mod ??= await _js.InvokeAsync<IJSObjectReference>("import", "/js/bff.js");
        return await _mod.InvokeAsync<string>("getCookie", "XSRF-TOKEN");
    }

    // ── GET ──────────────────────────────────────────────────────────────

    public Task<T?> GetAsync<T>(string path, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<T>(path, ct);

    // ── POST ─────────────────────────────────────────────────────────────

    public async Task<HttpResponseMessage> PostAsync<T>(string path, T body, CancellationToken ct = default)
    {
        var token = await GetXsrfAsync();
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("X-CSRF-TOKEN", token);
        return await _http.SendAsync(req, ct);
    }

    public async Task<HttpResponseMessage> PostMultipartAsync(string path, MultipartFormDataContent content, CancellationToken ct = default)
    {
        var token = await GetXsrfAsync();
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };
        req.Headers.Add("X-CSRF-TOKEN", token);
        return await _http.SendAsync(req, ct);
    }

    // ── PUT ──────────────────────────────────────────────────────────────

    public async Task<HttpResponseMessage> PutAsync<T>(string path, T body, CancellationToken ct = default)
    {
        var token = await GetXsrfAsync();
        using var req = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("X-CSRF-TOKEN", token);
        return await _http.SendAsync(req, ct);
    }

    // ── DELETE ────────────────────────────────────────────────────────────

    public async Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct = default)
    {
        var token = await GetXsrfAsync();
        using var req = new HttpRequestMessage(HttpMethod.Delete, path);
        req.Headers.Add("X-CSRF-TOKEN", token);
        return await _http.SendAsync(req, ct);
    }

    // ── PATCH ─────────────────────────────────────────────────────────────

    public async Task<HttpResponseMessage> PatchAsync<T>(string path, T body, CancellationToken ct = default)
    {
        var token = await GetXsrfAsync();
        using var req = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("X-CSRF-TOKEN", token);
        return await _http.SendAsync(req, ct);
    }

    // ── Typed response helpers ───────────────────────────────────────────

    /// <summary>
    /// Sends a mutating request and deserializes the JSON response body.
    /// Returns <c>default</c> when the response is not a success status code.
    /// </summary>
    public async Task<TResponse?> SendAsync<TBody, TResponse>(
        HttpMethod method, string path, TBody body, CancellationToken ct = default)
    {
        var token = await GetXsrfAsync();
        using var req = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("X-CSRF-TOKEN", token);

        var response = await _http.SendAsync(req, ct);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(ct);
    }
}
