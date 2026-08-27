// ABOUTME: Evicts output-cache responses whose HAL reporting affordances depend on tenant intake settings.
// ABOUTME: Uses the canonical event detail and collection cache tags in deterministic order.

using Explore.Application.Contracts.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Services;

public sealed class EventReportingOutputCacheInvalidator(IOutputCacheStore outputCacheStore)
    : IEventReportingOutputCacheInvalidator
{
    public async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        await outputCacheStore.EvictByTagAsync("detail-data", cancellationToken);
        await outputCacheStore.EvictByTagAsync("list-data", cancellationToken);
    }
}
