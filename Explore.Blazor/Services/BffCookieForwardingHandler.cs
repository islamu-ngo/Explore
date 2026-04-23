// ABOUTME: HTTP message handler that forwards the captured auth cookie to BFF self-endpoints.
// ABOUTME: Required because BffSelfClient uses UseCookies=false for handler pooling hygiene.

namespace Explore.Blazor.Services;

public class BffCookieForwardingHandler : DelegatingHandler
{
    private const string AntiforgeryCookieName = "XSRF-TOKEN";
    private const string AntiforgeryHeaderName = "X-CSRF-TOKEN";
    private readonly IBffAuthCookieStore _bffAuthCookieStore;
    private readonly ILogger<BffCookieForwardingHandler> _logger;

    public BffCookieForwardingHandler(
        IBffAuthCookieStore bffAuthCookieStore,
        ILogger<BffCookieForwardingHandler> logger)
    {
        _bffAuthCookieStore = bffAuthCookieStore;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var cookie = _bffAuthCookieStore.CookieHeader;
        if (!string.IsNullOrEmpty(cookie) && !request.Headers.Contains("Cookie"))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);

            var xsrfToken = TryGetCookieValue(cookie, AntiforgeryCookieName);
            if (!string.IsNullOrWhiteSpace(xsrfToken) && !request.Headers.Contains(AntiforgeryHeaderName))
            {
                request.Headers.TryAddWithoutValidation(AntiforgeryHeaderName, xsrfToken);
            }

            _logger.LogDebug("[BffCookieForwardingHandler] Forwarded auth cookie to {Path}", request.RequestUri?.PathAndQuery);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static string? TryGetCookieValue(string cookieHeader, string cookieName)
    {
        foreach (var segment in cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = segment[..separatorIndex];
            if (!string.Equals(name, cookieName, StringComparison.Ordinal))
            {
                continue;
            }

            var value = segment[(separatorIndex + 1)..];
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
