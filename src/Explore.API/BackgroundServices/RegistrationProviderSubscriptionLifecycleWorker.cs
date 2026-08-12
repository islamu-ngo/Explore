// ABOUTME: Thin hosted worker that triggers registration-provider subscription renewals and response sweeps.
// ABOUTME: Owns only the polling loop; Application service owns claim, provider I/O, and settlement behavior.

using Explore.Application.Services.Registration;

namespace Explore.API.BackgroundServices;

public sealed class RegistrationProviderSubscriptionLifecycleWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RegistrationProviderSubscriptionLifecycleWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Registration provider subscription lifecycle drain failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<RegistrationProviderSubscriptionLifecycleService>();
        return await service.DrainOnceAsync(cancellationToken);
    }
}
