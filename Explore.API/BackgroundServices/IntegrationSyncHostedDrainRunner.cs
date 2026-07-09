// ABOUTME: Runs one hosted-service integration sync drain cycle through the shared drain boundary.
// ABOUTME: Keeps timer orchestration separate from provider-specific Listmonk dispatch logic.

using Explore.Application.Contracts.Services;

namespace Explore.API.BackgroundServices;

public sealed class IntegrationSyncHostedDrainRunner(IServiceProvider serviceProvider)
{
    public async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var drainService = scope.ServiceProvider.GetRequiredService<IIntegrationSyncDrainService>();

        await drainService.ProcessBatchAsync(stoppingToken);
    }
}
