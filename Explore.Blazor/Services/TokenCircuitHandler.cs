// ABOUTME: Captures the access token from the initial HTTP request into the circuit-scoped token service.
// ABOUTME: Ensures Blazor Server circuit-dispatched events can resolve tokens even when HttpContext is null.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Explore.Blazor.Services;

public class TokenCircuitHandler : CircuitHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICircuitAccessTokenService _circuitAccessTokenService;
    private readonly ICircuitUserContext _circuitUserContext;
    private readonly IBffAuthCookieStore _bffAuthCookieStore;
    private readonly ILogger<TokenCircuitHandler> _logger;

    public TokenCircuitHandler(
        IHttpContextAccessor httpContextAccessor,
        ICircuitAccessTokenService circuitAccessTokenService,
        ICircuitUserContext circuitUserContext,
        IBffAuthCookieStore bffAuthCookieStore,
        ILogger<TokenCircuitHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitAccessTokenService = circuitAccessTokenService;
        _circuitUserContext = circuitUserContext;
        _bffAuthCookieStore = bffAuthCookieStore;
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("[TokenCircuitHandler] No authenticated user during circuit open - skipping token capture");
            return Task.CompletedTask;
        }

        try
        {
            // Capture userId into AsyncLocal-backed context so AccessTokenForwardingHandler
            // can resolve it even when running in a different DI scope.
            var userId = httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("sid")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _circuitUserContext.SetUserId(userId);
                _circuitUserContext.SetSessionId(httpContext.User.FindFirst("sid")?.Value);
                _logger.LogDebug("[TokenCircuitHandler] Captured userId {UserId} for circuit {CircuitId}",
                    userId, circuit.Id);
            }

            // Capture auth cookie so BffSelfClient can present it on BFF endpoints.
            if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                var cookieValue = cookieHeader.ToString();
                if (!string.IsNullOrEmpty(cookieValue))
                {
                    _bffAuthCookieStore.SetCookieHeader(cookieValue);
                    _logger.LogDebug("[TokenCircuitHandler] Captured auth cookie for circuit {CircuitId} (length: {Length})",
                        circuit.Id, cookieValue.Length);
                }
            }

            // Strategy 1: Read from HttpContext.Items where CaptureAccessTokenAsync middleware stored it.
            // This is the same hybrid pattern used by TenantRouteContextAccessor.
            string? token = null;
            if (httpContext.Items.TryGetValue("AccessToken", out var itemValue) && itemValue is string itemToken)
            {
                token = itemToken;
                _logger.LogDebug("[TokenCircuitHandler] Found access token in HttpContext.Items for circuit {CircuitId}",
                    circuit.Id);
            }

            // Strategy 2: Fall back to GetTokenAsync if middleware didn't capture it.
            if (string.IsNullOrEmpty(token))
            {
                token = httpContext.GetTokenAsync("access_token").GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("[TokenCircuitHandler] Retrieved access token via GetTokenAsync for circuit {CircuitId}",
                        circuit.Id);
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                _circuitAccessTokenService.SetToken(token);
                _logger.LogDebug("[TokenCircuitHandler] Captured access token for circuit {CircuitId} (length: {TokenLength})",
                    circuit.Id, token.Length);
            }
            else
            {
                _logger.LogDebug("[TokenCircuitHandler] No access_token found in HttpContext for circuit {CircuitId}",
                    circuit.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TokenCircuitHandler] Failed to capture access token during circuit open for circuit {CircuitId}",
                circuit.Id);
        }

        return Task.CompletedTask;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            using var userScope = _circuitUserContext.BeginActivityScope();
            using var cookieScope = _bffAuthCookieStore.BeginActivityScope();
            await next(context);
        };
    }
}
