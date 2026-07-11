// ABOUTME: Compatibility facade for the shared Event.Web.BffHosting proxy header sanitizer.
// ABOUTME: Keeps existing Explore.Blazor call sites while centralizing privileged header stripping.

using SharedBffProxyHeaderSanitizer = Event.Web.BffHosting.Security.BffProxyHeaderSanitizer;

namespace Explore.Blazor.Services;

public static class BffProxyHeaderSanitizer
{
    public static void RemoveBrowserControlledHeaders(HttpRequestMessage proxyRequest)
    {
        SharedBffProxyHeaderSanitizer.RemoveBrowserControlledHeaders(proxyRequest);
    }
}
