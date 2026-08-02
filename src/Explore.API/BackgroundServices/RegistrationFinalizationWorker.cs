// ABOUTME: Hosted polling loop for durable registration-finalization effects.
// ABOUTME: Invokes the shared fenced drain command from a fresh dependency-injection scope.

using Explore.Application.Features.RegistrationSubmissions.Commands;
using MediatR;

namespace Explore.API.BackgroundServices;

public sealed class RegistrationFinalizationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RegistrationFinalizationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

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
                logger.LogError(exception, "Registration-finalization drain cycle failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(
            new DrainRegistrationFinalizationEffectsCommand("registration-finalization-worker"),
            cancellationToken);
    }
}
