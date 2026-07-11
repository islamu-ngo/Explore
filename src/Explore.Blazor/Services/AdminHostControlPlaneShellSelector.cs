// ABOUTME: Selects the embedded control-plane shell for requests arriving on configured admin hosts.
// ABOUTME: Keeps the root Blazor host branch testable without coupling it to tenant routing internals.

using Event.Web.BffHosting.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Explore.Blazor.Services;

public sealed class AdminHostControlPlaneShellSelector(IEventBffHostClassifier hostClassifier)
{
    public bool ShouldUseControlPlaneShell(HttpContext? httpContext)
    {
        return httpContext is not null && hostClassifier.IsAdminHost(httpContext);
    }
}
