// ABOUTME: Svix-backed provider portal access service for webhook endpoint management.
// ABOUTME: Generates short-lived backend-only App Portal URLs without exposing Svix API tokens to clients.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Domain;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Svix;

namespace Explore.Infrastructure.Webhooks;

public sealed class SvixAppPortalService(
    ISvixWebhookClient svixClient,
    IWebhookConsumerRepository consumerRepository,
    IOptionsMonitor<WebhookOptions> options) : IWebhookProviderPortalService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MinExpiry = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MaxExpiry = TimeSpan.FromHours(1);

    public async Task<WebhookProviderPortalAccessResult> CreateAccessAsync(
        WebhookProviderPortalAccessInput input,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentOptions = options.CurrentValue;
        if (currentOptions.IsDisabled
            || !(currentOptions.IsProvider(WebhookOptions.ProviderSvix)
                 || currentOptions.IsProvider(WebhookOptions.ProviderComposite)))
        {
            return WebhookProviderPortalAccessResult.Failure(
                "svix_provider_not_enabled",
                isRetryable: false);
        }

        if (!currentOptions.Svix.AppPortalEnabled)
        {
            return WebhookProviderPortalAccessResult.Failure(
                "svix_app_portal_disabled",
                isRetryable: false);
        }

        if (string.IsNullOrWhiteSpace(input.SessionId))
        {
            return WebhookProviderPortalAccessResult.Failure(
                "webhook_portal_session_required",
                isRetryable: false);
        }

        try
        {
            var consumer = await ResolveConsumerAsync(input, cancellationToken);
            if (input.ConsumerId is not null && consumer is null)
            {
                return WebhookProviderPortalAccessResult.Failure(
                    "webhook_consumer_not_found",
                    isRetryable: false);
            }

            var app = await svixClient.GetOrCreateApplicationAsync(
                SvixWebhookApplicationMapper.CreateSyncRequest(input.TenantId, input.ConsumerId, consumer),
                cancellationToken);
            var expiresIn = NormalizeExpiry(input.ExpiresIn);
            var portal = await svixClient.CreateAppPortalAccessAsync(
                new SvixAppPortalAccessRequest(
                    input.TenantId,
                    app.AppUid,
                    input.SessionId.Trim(),
                    input.ReadOnly,
                    expiresIn,
                    NormalizeFeatureFlags(input.FeatureFlags),
                    $"svix-portal:{app.AppUid}:{input.SessionId.Trim()}"),
                cancellationToken);

            return WebhookProviderPortalAccessResult.Success(
                portal.Url,
                portal.Token,
                DateTimeOffset.UtcNow.Add(expiresIn));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SvixWebhookConfigurationException ex)
        {
            return WebhookProviderPortalAccessResult.Failure(ex.FailureCategory, isRetryable: false, ex.FailureCategory);
        }
        catch (ApiException ex)
        {
            var failure = SvixWebhookFailureClassifier.Classify(ex);
            return WebhookProviderPortalAccessResult.Failure(
                failure.Category,
                failure.IsRetryable,
                failure.SafeDetail);
        }
        catch (Exception ex)
        {
            return WebhookProviderPortalAccessResult.Failure(
                "svix_app_portal_failed",
                isRetryable: true,
                ex.GetType().Name);
        }
    }

    private async Task<WebhookConsumer?> ResolveConsumerAsync(
        WebhookProviderPortalAccessInput input,
        CancellationToken cancellationToken)
    {
        if (input.ConsumerId is not { } consumerId)
        {
            return null;
        }

        return await consumerRepository.GetByTenantAndIdAsync(
            input.TenantId,
            consumerId,
            cancellationToken);
    }

    private static TimeSpan NormalizeExpiry(TimeSpan? expiresIn)
    {
        var requested = expiresIn ?? DefaultExpiry;
        if (requested < MinExpiry)
        {
            return MinExpiry;
        }

        return requested > MaxExpiry ? MaxExpiry : requested;
    }

    private static IReadOnlyCollection<string> NormalizeFeatureFlags(IReadOnlyCollection<string> featureFlags) =>
        featureFlags
            .Select(flag => flag.Trim())
            .Where(flag => flag.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
}
