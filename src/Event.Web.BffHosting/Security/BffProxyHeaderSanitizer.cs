// ABOUTME: Sanitizes browser-originated credential headers before BFF proxy forwarding.
// ABOUTME: Ensures downstream API requests receive only server-owned privileged context.

namespace Event.Web.BffHosting.Security;

public static class BffProxyHeaderSanitizer
{
    private const int MaxCorrelationHeaderLength = 128;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string RequestIdHeader = "X-Request-ID";

    private static readonly string[] BrowserCredentialHeaderNames =
    [
        "Authorization",
        "Proxy-Authorization",
        "Cookie",
        EventBffHeaderNames.SetupSecret,
        EventBffHeaderNames.AtprotoBootstrapAssertion,
        EventBffHeaderNames.AtprotoSessionBridgeAssertion,
        EventBffHeaderNames.ApiKey,
        "Access-Token",
        "Refresh-Token",
        "Identity-Token",
        "Id-Token",
        "X-Access-Token",
        "X-Refresh-Token",
        "X-Identity-Token",
        "X-Id-Token",
        "X-Auth-Token"
    ];

    public static void RemoveBrowserControlledHeaders(HttpRequestMessage proxyRequest)
    {
        ArgumentNullException.ThrowIfNull(proxyRequest);

        proxyRequest.Headers.Authorization = null;

        foreach (var headerName in BrowserCredentialHeaderNames)
        {
            _ = proxyRequest.Headers.Remove(headerName);
        }

        _ = proxyRequest.Headers.Remove(EventBffHeaderNames.TenantId);
        _ = proxyRequest.Headers.Remove(EventBffHeaderNames.TenantSlug);

        var supportHeaderNames = proxyRequest.Headers
            .Select(header => header.Key)
            .Where(EventBffHeaderNames.IsSupportAccessHeader)
            .ToArray();

        foreach (var headerName in supportHeaderNames)
        {
            _ = proxyRequest.Headers.Remove(headerName);
        }

        RemoveUnsafeCorrelationMetadata(proxyRequest, CorrelationIdHeader);
        RemoveUnsafeCorrelationMetadata(proxyRequest, RequestIdHeader);
    }

    private static void RemoveUnsafeCorrelationMetadata(HttpRequestMessage request, string headerName)
    {
        if (!request.Headers.TryGetValues(headerName, out var values))
        {
            return;
        }

        var headerValues = values.ToArray();
        if (headerValues.Length != 1 || !IsSafeCorrelationValue(headerValues[0]))
        {
            _ = request.Headers.Remove(headerName);
        }
    }

    private static bool IsSafeCorrelationValue(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= MaxCorrelationHeaderLength
            && value.All(character => character is >= '!' and <= '~');
    }
}
