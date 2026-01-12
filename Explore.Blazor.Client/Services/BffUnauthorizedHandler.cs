using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public sealed class BffUnauthorizedHandler : DelegatingHandler
{
    private readonly NavigationManager _nav;
    private readonly ILogger<BffUnauthorizedHandler> _logger;

    public BffUnauthorizedHandler(NavigationManager nav, ILogger<BffUnauthorizedHandler> logger)
    {
        _nav = nav;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BFF handler: request failed for {Method} {Uri}", request.Method, request.RequestUri);
            throw;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Avoid redirecting when already navigating to login endpoints
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (!path.StartsWith("/login", System.StringComparison.OrdinalIgnoreCase) && !path.StartsWith("/auth", System.StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("BFF handler: received 401 for {Method} {Uri} - redirecting to /login", request.Method, request.RequestUri);

                // Force full page load to hit server login endpoint which triggers the BFF/OIDC challenge
                var returnUrl = Uri.EscapeDataString(_nav.ToBaseRelativePath(_nav.Uri));
                var loginUrl = $"/login?returnUrl={returnUrl}";

                // Navigate with full page load so the server can start auth flow
                _nav.NavigateTo(loginUrl, forceLoad: true);
            }
        }

        return response;
    }
}
