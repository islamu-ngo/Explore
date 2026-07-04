// ABOUTME: Defines the shared BFF adapter contract for trusted setup-secret forwarding.
// ABOUTME: Keeps bootstrap secret resolution host-owned while proxy replacement stays shared.

namespace Event.Web.BffHosting.Abstractions;

public interface IEventBffSetupSecretProvider
{
    ValueTask<string?> ResolveSetupSecretAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
