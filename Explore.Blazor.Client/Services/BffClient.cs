using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services;

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

    public Task<T?> GetAsync<T>(string path) =>
        _http.GetFromJsonAsync<T>(path);

    public async Task<HttpResponseMessage> PostAsync<T>(string path, T body)
    {
        var token = await GetXsrfAsync();
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("X-CSRF-TOKEN", token);
        return await _http.SendAsync(req);
    }

    public async Task<HttpResponseMessage> PostMultipartAsync(string path, MultipartFormDataContent content)
    {
        var token = await GetXsrfAsync();
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };
        req.Headers.Add("X-CSRF-TOKEN", token);
        return await _http.SendAsync(req);
    }
}
