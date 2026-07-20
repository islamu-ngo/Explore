// ABOUTME: Blocks API startup until retained platform privacy-erasure intents are replayed.
// ABOUTME: Preserves caller cancellation and exposes only sanitized fail-closed errors.

using Explore.Application.Configuration;
using Explore.Application.Contracts.Services;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public static class PrivacyErasureStartupGate
{
    public static async Task RunAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        PrivacyErasureDurabilityOptions options = scope.ServiceProvider
            .GetRequiredService<IOptions<PrivacyErasureDurabilityOptions>>()
            .Value;
        if (options.Mode == PrivacyErasureDurabilityMode.ApplicationDatabase)
        {
            return;
        }

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
                $"Retained privacy-erasure replay failed ({exception.GetType().Name}); API startup is blocked.");
        }
    }
}
