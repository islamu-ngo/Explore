// ABOUTME: Defines the host boundary for evicting public ATProto discovery response caches.
// ABOUTME: Lets ingestion and settings mutations invalidate HTTP caches without depending on ASP.NET Core.

namespace Explore.Application.Contracts.Infrastructure;

public interface IAtprotoDiscoveryCacheInvalidator
{
    ValueTask InvalidateAsync(CancellationToken cancellationToken = default);
}
