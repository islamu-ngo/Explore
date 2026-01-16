using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Services;

public sealed class ServerCookieForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ServerCookieForwardingHandler> _logger;

    public ServerCookieForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ServerCookieForwardingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        var cookieHeader = context.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrEmpty(cookieHeader) && !request.Headers.Contains("Cookie"))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
