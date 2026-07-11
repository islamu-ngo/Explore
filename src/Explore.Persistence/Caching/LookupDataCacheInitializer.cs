using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace Explore.Persistence.Caching;

public class LookupDataCacheInitializer : IHostedService
{
    private readonly ILookupDataCache _cache;

    public LookupDataCacheInitializer(ILookupDataCache cache)
    {
        _cache = cache;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _cache.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
