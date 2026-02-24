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

    // ── GET ──────────────────────────────────────────────────────────────

    public Task<T?> GetAsync<T>(string path, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<T>(path, ct);

    // ── Mutating verbs ───────────────────────────────────────────────────

    public Task<HttpResponseMessage> PostAsync<T>(string path, T body, CancellationToken ct = default) =>
        SendMutatingAsync(HttpMethod.Post, path, JsonContent.Create(body), ct);

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
        var token = await GetXsrfAsync();
        using var req = new HttpRequestMessage(method, path) { Content = content };
        req.Headers.Add("X-CSRF-TOKEN", token);
        return await _http.SendAsync(req, ct);
    }

    private async Task<string> GetXsrfAsync()
    {
        _mod ??= await _js.InvokeAsync<IJSObjectReference>("import", "/js/bff.js");
        return await _mod.InvokeAsync<string>("getCookie", "XSRF-TOKEN");
    }
}
