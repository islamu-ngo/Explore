// ABOUTME: Sanitizes browser-originated credential headers before BFF proxy forwarding.
// ABOUTME: Ensures downstream API requests receive only server-owned privileged context.

namespace Event.Web.BffHosting.Security;

public static class BffProxyHeaderSanitizer
{
    private static readonly string[] BrowserCredentialHeaderNames =
    [
        "Authorization",
        "Proxy-Authorization",
        "Cookie",
        EventBffHeaderNames.SetupSecret,
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
    }
}
