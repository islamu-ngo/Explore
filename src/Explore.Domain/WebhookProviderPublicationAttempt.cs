// ABOUTME: Append-only safe evidence for one provider publication or reconciliation action.
// ABOUTME: Records fences, outcomes, and bounded failure metadata without payloads or raw provider errors.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class WebhookProviderPublicationAttempt : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; private set; }
    public Guid WebhookProviderPublicationId { get; private set; }
    public WebhookProviderPublication? WebhookProviderPublication { get; private set; }
    public int AttemptNumber { get; private set; }
    public long PublicationFence { get; private set; }
    public WebhookProviderPublicationAttemptOutcome Outcome { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public string? ExternalProviderMessageId { get; private set; }
    public string? FailureCategory { get; private set; }
    public string? SafeDetail { get; private set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    private WebhookProviderPublicationAttempt()
    {
    }

    internal static WebhookProviderPublicationAttempt Create(
        Guid tenantId,
        Guid publicationId,
        int attemptNumber,
        long publicationFence,
        WebhookProviderPublicationAttemptOutcome outcome,
        DateTime startedAt,
        DateTime recordedAt,
        string? externalProviderMessageId = null,
        string? failureCategory = null,
        string? safeDetail = null)
    {
        if (attemptNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber));
        }

        if (publicationFence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(publicationFence));
        }

        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        return new WebhookProviderPublicationAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            WebhookProviderPublicationId = publicationId,
            AttemptNumber = attemptNumber,
            PublicationFence = publicationFence,
            Outcome = outcome,
            StartedAt = startedAt,
            RecordedAt = recordedAt,
            ExternalProviderMessageId = WebhookProviderPublication.NormalizeOptional(
                externalProviderMessageId,
                WebhookProviderPublication.MaxExternalProviderMessageIdLength,
                nameof(externalProviderMessageId)),
            FailureCategory = WebhookProviderPublication.NormalizeOptional(
                failureCategory,
                WebhookProviderPublication.MaxFailureCategoryLength,
                nameof(failureCategory)),
            SafeDetail = WebhookProviderPublication.BoundSafeDetail(safeDetail),
            CreatedAt = recordedAt
        };
    }
}

public enum WebhookProviderPublicationAttemptOutcome
{
    PublishingStarted = 1,
    ProviderQueued = 2,
    RetryScheduled = 3,
    PublicationUnknown = 4,
    DeadLettered = 5,
    AutomaticReconciliationStarted = 6,
    AutomaticReconciliationUnresolved = 7,
    ManualReconciliationRequired = 8,
    ReconciledProviderQueued = 9,
    Abandoned = 10
}
