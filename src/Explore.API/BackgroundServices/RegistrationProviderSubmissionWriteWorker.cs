// ABOUTME: Hosted polling loop for outbound provider submission write effects.
// ABOUTME: Runs the fenced drain command from a fresh dependency-injection scope.

using Explore.Application.Services.Registration.Commands;
using MediatR;

namespace Explore.API.BackgroundServices;

public sealed class RegistrationProviderSubmissionWriteWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RegistrationProviderSubmissionWriteWorker> logger) : BackgroundService
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
                logger.LogError(exception, "Registration provider submission write drain cycle failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(
            new DrainRegistrationProviderSubmissionWriteEffectsCommand("registration-provider-submission-write-worker"),
            cancellationToken);
    }
}
