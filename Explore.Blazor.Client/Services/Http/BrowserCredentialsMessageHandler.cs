using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services.Http;

/// <summary>
/// Message handler that ensures cookies/credentials are sent with all HTTP requests in WASM.
/// This is required for the BFF pattern where authentication is cookie-based.
/// </summary>
public class BrowserCredentialsMessageHandler : DelegatingHandler
{
    private readonly ILogger<BrowserCredentialsMessageHandler>? _logger;
    private readonly IJSRuntime? _jsRuntime;
    private IJSObjectReference? _module;

    public BrowserCredentialsMessageHandler()
    {
        // Default constructor for cases where logger is not available
    }

    public BrowserCredentialsMessageHandler(ILogger<BrowserCredentialsMessageHandler> logger)
    {
        _logger = logger;
    }

    public BrowserCredentialsMessageHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public BrowserCredentialsMessageHandler(ILogger<BrowserCredentialsMessageHandler> logger, IJSRuntime jsRuntime)
    {
        _logger = logger;
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Enable sending cookies with the request (required for authentication)
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        // Ensure the request mode allows requests (SameOrigin for BFF)
        request.SetBrowserRequestMode(BrowserRequestMode.SameOrigin);

        if (IsMutatingMethod(request.Method) && !request.Headers.Contains("X-CSRF-TOKEN"))
        {
            var token = await TryGetXsrfAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Add("X-CSRF-TOKEN", token);
            }
        }

        _logger?.LogDebug("[WASM HTTP] {Method} {Uri} - Credentials: Include, Mode: SameOrigin",
            request.Method, request.RequestUri?.PathAndQuery);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            _logger?.LogDebug("[WASM HTTP] {Method} {Uri} - Response: {StatusCode}",
                request.Method, request.RequestUri?.PathAndQuery, (int)response.StatusCode);

            // Log warning for auth-related failures
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger?.LogWarning("[WASM HTTP] {Method} {Uri} - Unauthorized (401). User may need to re-authenticate.",
                    request.Method, request.RequestUri?.PathAndQuery);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WASM HTTP] {Method} {Uri} - Request failed",
                request.Method, request.RequestUri?.PathAndQuery);
            throw;
        }
    }

    private static bool IsMutatingMethod(HttpMethod method)
        => method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch || method == HttpMethod.Delete;

    private async Task<string?> TryGetXsrfAsync(CancellationToken cancellationToken)
    {
        if (_jsRuntime is null)
        {
            return null;
        }

        try
        {
            _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, "/js/bff.js");
            return await _module.InvokeAsync<string?>("getCookie", cancellationToken, "XSRF-TOKEN");
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "[WASM HTTP] Could not read XSRF-TOKEN cookie for antiforgery header injection.");
            return null;
        }
    }
}
