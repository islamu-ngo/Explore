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
        CaptureCookieHeader(httpContext, circuit.Id);

        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("[TokenCircuitHandler] No authenticated user during circuit open - skipping identity/token capture");
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

    private void CaptureCookieHeader(HttpContext? httpContext, string circuitId)
    {
        // BffSelfClient uses the captured cookie header for server-side calls back through
        // BFF endpoints. First-run setup is unauthenticated, so capture this before the
        // authenticated-user token path exits.
        if (httpContext?.Request.Headers.TryGetValue("Cookie", out var cookieHeader) != true)
        {
            return;
        }

        var cookieValue = cookieHeader.ToString();
        if (string.IsNullOrEmpty(cookieValue))
        {
            return;
        }

        _bffAuthCookieStore.SetCookieHeader(cookieValue);
        _logger.LogDebug("[TokenCircuitHandler] Captured BFF cookie header for circuit {CircuitId} (length: {Length})",
            circuitId, cookieValue.Length);
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            CaptureCookieHeader(_httpContextAccessor.HttpContext, "inbound activity");
            using var userScope = _circuitUserContext.BeginActivityScope();
            using var cookieScope = _bffAuthCookieStore.BeginActivityScope();
            await next(context);
        };
    }
}
