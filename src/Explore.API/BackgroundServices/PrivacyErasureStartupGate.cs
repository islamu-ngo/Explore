// ABOUTME: Blocks API startup until external platform privacy-erasure intents are replayed.
// ABOUTME: Preserves caller cancellation and exposes only sanitized fail-closed errors.

using Explore.Application.Contracts.Services;

namespace Explore.API.BackgroundServices;

public static class PrivacyErasureStartupGate
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
                .GetRequiredService<IPrivacyErasureReplayService>()
                .ReplayAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"External privacy-erasure replay failed ({exception.GetType().Name}); API startup is blocked.");
        }
    }
}
