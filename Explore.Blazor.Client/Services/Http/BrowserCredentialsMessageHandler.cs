using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services.Http;

/// <summary>
/// Message handler that ensures cookies/credentials are sent with all HTTP requests in WASM.
/// This is required for the BFF pattern where authentication is cookie-based.
/// </summary>
public class BrowserCredentialsMessageHandler : DelegatingHandler
{
    private readonly ILogger<BrowserCredentialsMessageHandler>? _logger;

    public BrowserCredentialsMessageHandler()
    {
        // Default constructor for cases where logger is not available
    }

    public BrowserCredentialsMessageHandler(ILogger<BrowserCredentialsMessageHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Enable sending cookies with the request (required for authentication)
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        // Ensure the request mode allows requests (SameOrigin for BFF)
        request.SetBrowserRequestMode(BrowserRequestMode.SameOrigin);

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
}
