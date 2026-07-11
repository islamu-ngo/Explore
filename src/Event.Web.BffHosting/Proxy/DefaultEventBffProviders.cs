// ABOUTME: Provides safe no-op/default adapter implementations for optional BFF proxy context.
// ABOUTME: Lets hosts opt into tenant/setup/support forwarding explicitly through their own adapters.

using Event.Web.BffHosting.Abstractions;
using Event.Web.BffHosting.Security;
using Microsoft.AspNetCore.Authentication;

namespace Event.Web.BffHosting.Proxy;

internal sealed class AuthenticationTicketEventBffAccessTokenProvider : IEventBffAccessTokenProvider
{
    public async ValueTask<string?> ResolveAccessTokenAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var token = await httpContext.GetTokenAsync("access_token");
        return EventBffTokenSafety.IsTokenForwardable(token) ? token : null;
    }
}

internal sealed class NoopEventBffTenantHintProvider : IEventBffTenantHintProvider
{
    public string? ResolveTenantSlug(HttpContext httpContext) => null;
}

internal sealed class NoopEventBffSetupSecretProvider : IEventBffSetupSecretProvider
{
    public ValueTask<string?> ResolveSetupSecretAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);
}

internal sealed class NoopEventBffSupportAccessProvider : IEventBffSupportAccessProvider
{
    public ValueTask<string?> ResolveSupportAccessSessionIdAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);
}
