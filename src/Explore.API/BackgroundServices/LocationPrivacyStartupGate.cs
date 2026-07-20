// ABOUTME: Blocks API startup until retained location-erasure intents are replayed.
// ABOUTME: Preserves caller cancellation and exposes only sanitized fail-closed errors.

using Explore.Application.Contracts.Services;

namespace Explore.API.BackgroundServices;

public static class LocationPrivacyStartupGate
{
    public static async Task RunAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider
                .GetRequiredService<ILocationErasureReplayService>()
                .ReplayAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Retained location-erasure replay failed ({exception.GetType().Name}); API startup is blocked.");
        }
    }
}
