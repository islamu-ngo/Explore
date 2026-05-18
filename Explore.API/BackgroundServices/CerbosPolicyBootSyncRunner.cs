// ABOUTME: Runs the one-shot Cerbos policy package boot synchronization through the provider-neutral package service.
// ABOUTME: Gates startup publishing on complete Admin API configuration without logging endpoints or credentials.

using Explore.Application.Contracts.Infrastructure;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

/// <summary>
/// Executes a single boot-time Cerbos policy package publish when Admin API configuration is complete.
/// </summary>
public sealed class CerbosPolicyBootSyncRunner(
    IServiceScopeFactory scopeFactory,
    IOptions<CerbosAdminApiSettings> adminApiSettings,
    IOptions<CerbosPolicyBootSyncOptions> options,
    ILogger<CerbosPolicyBootSyncRunner> logger)
{
    private readonly CerbosAdminApiSettings _adminApiSettings = adminApiSettings.Value;
    private readonly CerbosPolicyBootSyncOptions _options = options.Value;

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Cerbos policy boot sync is disabled");
            return;
        }

        if (!HasAnyAdminEndpoint())
        {
            logger.LogDebug("Cerbos policy boot sync skipped because no Admin API endpoint is configured");
            return;
        }

        if (!HasCompleteAdminCredentials())
        {
            logger.LogWarning(
                "Cerbos policy boot sync skipped because Admin API credentials are incomplete. " +
                "Configure both username and password to enable zero-touch package publishing.");
            return;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(GetTimeout());

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var policyPackageService = scope.ServiceProvider.GetRequiredService<IPolicyPackageService>();

            var result = await policyPackageService.PublishAsync(timeoutCts.Token).ConfigureAwait(false);
            if (result.Succeeded)
            {
                logger.LogInformation(
                    "Cerbos policy boot sync published package {PackageId} with content hash {ContentHash}",
                    result.PackageId,
                    result.ContentHash);
                return;
            }

            logger.LogWarning(
                "Cerbos policy boot sync did not publish package {PackageId}: {Message}. Warnings={Warnings}",
                result.PackageId,
                result.Message,
                result.Warnings);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Cerbos policy boot sync cancelled during shutdown");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "Cerbos policy boot sync timed out after {TimeoutSeconds}s",
                GetTimeout().TotalSeconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cerbos policy boot sync failed with an unexpected error");
        }
    }

    private bool HasAnyAdminEndpoint()
    {
        return _adminApiSettings.Endpoints.Any(endpoint => !string.IsNullOrWhiteSpace(endpoint));
    }

    private bool HasCompleteAdminCredentials()
    {
        return !string.IsNullOrWhiteSpace(_adminApiSettings.AdminUsername) &&
            !string.IsNullOrWhiteSpace(_adminApiSettings.AdminPassword);
    }

    private TimeSpan GetTimeout()
    {
        return TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
    }
}
