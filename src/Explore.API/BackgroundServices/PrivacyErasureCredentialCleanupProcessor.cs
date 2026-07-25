// ABOUTME: Periodically destroys expired privacy-erasure receipt and provider locator credentials.
// ABOUTME: Emits only aggregate counts and delegates all mutation to the bounded cleanup service.

using Explore.Application.Configuration;
using Explore.Application.Contracts.Services;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class PrivacyErasureCredentialCleanupProcessor(
    IServiceProvider serviceProvider,
    IOptions<PrivacyErasureOptions> options,
    ILogger<PrivacyErasureCredentialCleanupProcessor> logger) : BackgroundService
{
    private readonly PrivacyErasureOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.RetentionCleanupEnabled)
        {
            logger.LogInformation("Privacy-erasure credential cleanup processor is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<IPrivacyErasureCredentialCleanupService>();
                var result = await cleanupService.CleanupAsync(DateTime.UtcNow, stoppingToken);
                logger.LogInformation(
                    "Privacy-erasure credential cleanup completed. DryRun={DryRun}, ReceiptEligible={ReceiptEligible}, ReceiptCleared={ReceiptCleared}, LocatorEligible={LocatorEligible}, LocatorCleared={LocatorCleared}.",
                    result.DryRun,
                    result.ReceiptHashesEligible,
                    result.ReceiptHashesCleared,
                    result.ProviderLocatorsEligible,
                    result.ProviderLocatorsCleared);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                logger.LogError("Privacy-erasure credential cleanup pass failed.");
            }

            await Task.Delay(_options.ProviderPollingInterval, stoppingToken);
        }
    }
}
