using System.Net.Http;
using System.Net.Http.Json;
using Explore.Blazor.Client.Contracts.Services;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services.Http;

/// <summary>
/// BFF HTTP facade for browser-side same-origin calls that rely on the shared WASM handler pipeline.
/// BrowserCredentialsMessageHandler adds credentials and antiforgery headers for mutating requests.
/// </summary>
public interface IBffClient
{
    Task<T?> GetAsync<T>(string path, CancellationToken ct = default);
    Task<T?> GetWithRegistrationOrderCapabilityAsync<T>(
        string path,
        string? capability,
        CancellationToken ct = default);
    Task<T?> GetWithTicketTransferCapabilityAsync<T>(
        string path,
        string? capability,
        CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync<T>(string path, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync(string path, CancellationToken ct = default);
    Task<HttpResponseMessage> PutAsync<T>(string path, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> PatchAsync<T>(string path, T body, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct = default);
    Task<HttpResponseMessage> PostMultipartAsync(string path, MultipartFormDataContent content, CancellationToken ct = default);
    Task<TResponse?> SendAsync<TBody, TResponse>(HttpMethod method, string path, TBody body, CancellationToken ct = default);
    Task<TResponse?> SendAsync<TResponse>(HttpMethod method, string path, CancellationToken ct = default);
    Task<TResponse?> SendIdempotentAsync<TResponse>(
        HttpMethod method,
        string path,
        Guid operationId,
        CancellationToken ct = default);
    Task<TResponse?>
        SendWithTicketTransferCapabilityAsync<
            TBody,
            TResponse>(
                HttpMethod method,
                string path,
                TBody body,
                string? capability,
                CancellationToken ct = default);
    Task<BffRegistrationPaymentCheckoutTicketResponseDto?> IssueRegistrationPaymentCheckoutTicketAsync(
        string path, string? guestCapability, CancellationToken ct = default);
}

public sealed class BffClient : IBffClient, IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _jsRuntime;
    private readonly SemaphoreSlim _checkoutIssueLock = new(1, 1);
    private IJSObjectReference? _module;
    private string? _activeCheckoutOperationId;

    public BffClient(HttpClient http, IJSRuntime jsRuntime)
    {
        _http = http;
        _jsRuntime = jsRuntime;
    }

    // ── GET ──────────────────────────────────────────────────────────────

    public Task<T?> GetAsync<T>(string path, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<T>(path, ct);

    public async Task<T?>
        GetWithRegistrationOrderCapabilityAsync<T>(
            string path,
            string? capability,
            CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            path);
        if (!string.IsNullOrWhiteSpace(capability))
        {
            request.Headers.Add(
                "X-Registration-Order-Capability",
                capability);
        }
        using HttpResponseMessage response =
            await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(
            ct);
    }

    public async Task<T?>
        GetWithTicketTransferCapabilityAsync<T>(
            string path,
            string? capability,
            CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            path);
        AddTicketTransferCapability(
            request,
            capability);
        using HttpResponseMessage response =
            await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(
            ct);
    }

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

    public async Task<TResponse?> SendAsync<TResponse>(
        HttpMethod method, string path, CancellationToken ct = default)
    {
        using var response = await SendMutatingAsync(method, path, content: null, ct);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(ct);
    }

    public async Task<TResponse?>
        SendIdempotentAsync<TResponse>(
            HttpMethod method,
            string path,
            Guid operationId,
            CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            method,
            path);
        request.Headers.Add(
            "Idempotency-Key",
            operationId.ToString("D"));
        using HttpResponseMessage response =
            await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }
        return await response.Content
            .ReadFromJsonAsync<TResponse>(ct);
    }

    public async Task<TResponse?>
        SendWithTicketTransferCapabilityAsync<
            TBody,
            TResponse>(
                HttpMethod method,
                string path,
                TBody body,
                string? capability,
                CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            method,
            path)
        {
            Content = JsonContent.Create(body),
        };
        AddTicketTransferCapability(
            request,
            capability);
        using HttpResponseMessage response =
            await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content
            .ReadFromJsonAsync<TResponse>(ct);
    }

    public async Task<BffRegistrationPaymentCheckoutTicketResponseDto?> IssueRegistrationPaymentCheckoutTicketAsync(
        string path,
        string? guestCapability,
        CancellationToken ct = default)
    {
        string operationId = Guid.CreateVersion7().ToString("D");
        string? previousOperationId;
        await _checkoutIssueLock.WaitAsync(ct);
        try
        {
            _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ct, "/js/bff.js");
            previousOperationId = _activeCheckoutOperationId;
            _activeCheckoutOperationId = operationId;
        }
        finally
        {
            _checkoutIssueLock.Release();
        }

        if (previousOperationId is not null)
        {
            await AbortCheckoutIssueAsync(previousOperationId);
        }

        try
        {
            return await _module.InvokeAsync<BffRegistrationPaymentCheckoutTicketResponseDto?>(
                "issueRegistrationPaymentCheckoutTicket",
                ct,
                path,
                guestCapability,
                operationId);
        }
        catch (OperationCanceledException)
        {
            await AbortCheckoutIssueAsync(operationId);
            throw;
        }
        finally
        {
            await _checkoutIssueLock.WaitAsync(CancellationToken.None);
            try
            {
                if (string.Equals(_activeCheckoutOperationId, operationId, StringComparison.Ordinal))
                {
                    _activeCheckoutOperationId = null;
                }
            }
            finally
            {
                _checkoutIssueLock.Release();
            }
        }
    }

    // ── Core send ────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> SendMutatingAsync(
        HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, path) { Content = content };
        return await _http.SendAsync(req, ct);
    }

    private static void AddTicketTransferCapability(
        HttpRequestMessage request,
        string? capability)
    {
        if (!string.IsNullOrWhiteSpace(capability))
        {
            request.Headers.Add(
                "X-Ticket-Transfer-Capability",
                capability);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_activeCheckoutOperationId is not null)
        {
            await AbortCheckoutIssueAsync(_activeCheckoutOperationId);
            _activeCheckoutOperationId = null;
        }
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
        _checkoutIssueLock.Dispose();
    }

    private async Task AbortCheckoutIssueAsync(string operationId)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("abortRegistrationPaymentCheckoutTicket", operationId);
        }
    }
}
