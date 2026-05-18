using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services.Http;

/// <summary>
/// Message handler that ensures CSRF tokens are attached to mutating HTTP requests in WASM.
/// </summary>
public class BffAntiforgeryMessageHandler : DelegatingHandler
{
    private readonly ILogger<BffAntiforgeryMessageHandler>? _logger;
    private readonly IJSRuntime? _jsRuntime;
    private IJSObjectReference? _module;

    public BffAntiforgeryMessageHandler()
    {
    }

    public BffAntiforgeryMessageHandler(ILogger<BffAntiforgeryMessageHandler> logger)
    {
        _logger = logger;
    }

    public BffAntiforgeryMessageHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public BffAntiforgeryMessageHandler(ILogger<BffAntiforgeryMessageHandler> logger, IJSRuntime jsRuntime)
    {
        _logger = logger;
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (IsMutatingMethod(request.Method) && !request.Headers.Contains("X-CSRF-TOKEN"))
        {
            var token = await TryGetXsrfAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Add("X-CSRF-TOKEN", token);
                _logger?.LogDebug("[WASM HTTP] Attached X-CSRF-TOKEN to {Method} {Uri}", request.Method, request.RequestUri?.PathAndQuery);
            }
        }

        return await base.SendAsync(request, cancellationToken);
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
