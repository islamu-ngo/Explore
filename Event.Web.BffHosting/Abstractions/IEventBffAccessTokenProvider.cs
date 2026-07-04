// ABOUTME: Defines the shared BFF adapter contract for resolving server-held access tokens.
// ABOUTME: Lets host apps supply circuit/session-aware token lookup without coupling the library to them.

namespace Event.Web.BffHosting.Abstractions;

public interface IEventBffAccessTokenProvider
{
    ValueTask<string?> ResolveAccessTokenAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
