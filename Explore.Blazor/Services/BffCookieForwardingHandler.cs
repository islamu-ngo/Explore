// ABOUTME: HTTP message handler that forwards the captured auth cookie to BFF self-endpoints.
// ABOUTME: Required because BffSelfClient uses UseCookies=false for handler pooling hygiene.

using Microsoft.AspNetCore.Http;

namespace Explore.Blazor.Services;

public class BffCookieForwardingHandler : DelegatingHandler
{
    private const string AntiforgeryCookieName = "XSRF-TOKEN";
    private const string FrameworkAntiforgeryCookiePrefix = ".AspNetCore.Antiforgery.";
    private const string AntiforgeryHeaderName = "X-CSRF-TOKEN";
    private readonly IBffAuthCookieStore _bffAuthCookieStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BffCookieForwardingHandler> _logger;

    public BffCookieForwardingHandler(
        IBffAuthCookieStore bffAuthCookieStore,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BffCookieForwardingHandler> logger)
    {
        _bffAuthCookieStore = bffAuthCookieStore;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public BffCookieForwardingHandler(
        IBffAuthCookieStore bffAuthCookieStore,
        ILogger<BffCookieForwardingHandler> logger)
        : this(bffAuthCookieStore, new HttpContextAccessor(), logger)
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var currentRequestCookie = _httpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        var cookie = BuildForwardedCookieHeader(_bffAuthCookieStore.CookieHeader, currentRequestCookie);

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

    private static string? BuildForwardedCookieHeader(string? circuitCookie, string? currentRequestCookie)
    {
        if (string.IsNullOrWhiteSpace(circuitCookie))
        {
            return string.IsNullOrWhiteSpace(currentRequestCookie) ? null : currentRequestCookie;
        }

        if (string.IsNullOrWhiteSpace(currentRequestCookie))
        {
            return circuitCookie;
        }

        var mergedCookies = ParseCookies(circuitCookie);
        foreach (var currentCookie in ParseCookies(currentRequestCookie))
        {
            var existingIndex = mergedCookies.FindIndex(cookie => string.Equals(cookie.Name, currentCookie.Name, StringComparison.Ordinal));
            if (existingIndex < 0)
            {
                mergedCookies.Add(currentCookie);
                continue;
            }

            if (IsAntiforgeryCookie(currentCookie.Name))
            {
                mergedCookies[existingIndex] = currentCookie;
            }
        }

        return string.Join("; ", mergedCookies.Select(cookie => $"{cookie.Name}={cookie.Value}"));
    }

    private static bool IsAntiforgeryCookie(string cookieName)
    {
        return string.Equals(cookieName, AntiforgeryCookieName, StringComparison.Ordinal)
            || cookieName.StartsWith(FrameworkAntiforgeryCookiePrefix, StringComparison.Ordinal);
    }

    private static List<CookieValue> ParseCookies(string cookieHeader)
    {
        var cookies = new List<CookieValue>();
        foreach (var segment in cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = segment[..separatorIndex];
            var value = segment[(separatorIndex + 1)..];
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            cookies.Add(new CookieValue(name, value));
        }

        return cookies;
    }

    private readonly record struct CookieValue(string Name, string Value);
}
