// ABOUTME: Application-layer contract for provider-hosted webhook management portal access.
// ABOUTME: Keeps backend portal URL generation provider-neutral and hides provider SDK details from API/UI layers.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookProviderPortalService
{
    Task<WebhookProviderPortalAccessResult> CreateAccessAsync(
        WebhookProviderPortalAccessInput input,
        CancellationToken cancellationToken);
}

public sealed record WebhookProviderPortalAccessInput(
    Guid TenantId,
    Guid? ConsumerId,
    string SessionId,
    bool ReadOnly,
    TimeSpan? ExpiresIn,
    IReadOnlyCollection<string> FeatureFlags);

public sealed record WebhookProviderPortalAccessResult(
    bool Succeeded,
    string? Url,
    string? Token,
    DateTimeOffset? ExpiresAt,
    bool IsRetryable,
    string? FailureCategory,
    string? SafeDetail)
{
    public static WebhookProviderPortalAccessResult Success(
        string url,
        string? token,
        DateTimeOffset expiresAt) =>
        new(true, url, token, expiresAt, false, null, null);

    public static WebhookProviderPortalAccessResult Failure(
        string failureCategory,
        bool isRetryable,
        string? safeDetail = null) =>
        new(false, null, null, null, isRetryable, failureCategory, safeDetail);
}
