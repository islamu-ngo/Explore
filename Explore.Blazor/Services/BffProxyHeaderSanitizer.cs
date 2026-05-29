// ABOUTME: Sanitizes browser-originated credential headers before YARP forwards API requests.
// ABOUTME: Ensures the BFF proxy forwards only server-owned auth, tenant, and setup-secret context.

using Explore.Application.Constants;

namespace Explore.Blazor.Services;

public static class BffProxyHeaderSanitizer
{
    private static readonly string[] BrowserCredentialHeaderNames =
    [
        "Authorization",
        "Proxy-Authorization",
        "Cookie",
        "X-Setup-Secret",
        ApiAuthenticationHeaderNames.ApiKey,
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

        _ = proxyRequest.Headers.Remove(TenantHeaderNames.TenantId);
        _ = proxyRequest.Headers.Remove(TenantHeaderNames.TenantSlug);
    }
}
