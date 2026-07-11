// ABOUTME: Defines the shared BFF adapter contract for trusted support-access forwarding.
// ABOUTME: Keeps support-access session storage host-owned while shared proxy code injects only safe headers.

namespace Event.Web.BffHosting.Abstractions;

public interface IEventBffSupportAccessProvider
{
    ValueTask<string?> ResolveSupportAccessSessionIdAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
