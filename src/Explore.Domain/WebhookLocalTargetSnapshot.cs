// ABOUTME: Immutable Local-provider endpoint target captured for one outgoing webhook delivery plan.
// ABOUTME: Freezes destination, endpoint configuration, signing-key reference, and delivery limits without storing secret values.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class WebhookLocalTargetSnapshot : ITenantEntity, IAuditableEntity
{
    public const int MaxDestinationUrlLength = 2_048;
    public const int MaxCredentialReferenceLength = 500;

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; private set; } = null!;
    public Guid WebhookMessageId { get; private set; }
    public WebhookMessage WebhookMessage { get; private set; } = null!;
    public Guid DeliveryPlanSnapshotId { get; private set; }
    public WebhookDeliveryPlanSnapshot DeliveryPlanSnapshot { get; private set; } = null!;
    public Guid WebhookEndpointId { get; private set; }
    public WebhookEndpoint WebhookEndpoint { get; private set; } = null!;
    public int EndpointConfigurationVersion { get; private set; }
    public string DestinationUrl { get; private set; } = string.Empty;
    public string CredentialReference { get; private set; } = string.Empty;
    public int CredentialVersion { get; private set; }
    public DateTimeOffset CredentialValidFromUtc { get; private set; }
    public DateTimeOffset? CredentialValidUntilUtc { get; private set; }
    public int MaxAttempts { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public int? RateLimitPerMinute { get; private set; }
    public DateTimeOffset CapturedAtUtc { get; private set; }
    public int DeliveryStatusId { get; private set; }
    public WebhookLocalDeliveryStatusLookup DeliveryStatusLookup { get; private set; } = null!;
    [NotMapped]
    public WebhookLocalDeliveryStatus DeliveryStatus
    {
        get => (WebhookLocalDeliveryStatus)DeliveryStatusId;
        private set => DeliveryStatusId = (int)value;
    }
    public DateTimeOffset NextActionAtUtc { get; private set; }
    public string? ProcessingLeaseOwner { get; private set; }
    public Guid? ProcessingLeaseToken { get; private set; }
    public DateTimeOffset? ProcessingLeaseExpiresAtUtc { get; private set; }
    public long DeliveryFence { get; private set; }
    public long ConcurrencyVersion { get; private set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static WebhookLocalTargetSnapshot Create(
        WebhookDeliveryPlanSnapshot deliveryPlanSnapshot,
        WebhookEndpoint webhookEndpoint,
        int endpointConfigurationVersion,
        DateTimeOffset credentialValidFromUtc,
        DateTimeOffset? credentialValidUntilUtc,
        DateTimeOffset capturedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(deliveryPlanSnapshot);
        ArgumentNullException.ThrowIfNull(webhookEndpoint);

        if (deliveryPlanSnapshot.ProviderMode is not (WebhookProviderMode.Local or WebhookProviderMode.Composite))
        {
            throw new InvalidOperationException(
                "A local target requires a Local or Composite delivery plan.");
        }

        if (webhookEndpoint.TenantId != deliveryPlanSnapshot.TenantId ||
            webhookEndpoint.ConsumerId != deliveryPlanSnapshot.WebhookConsumerId)
        {
            throw new InvalidOperationException(
                "The endpoint must belong to the delivery plan tenant and consumer.");
        }

        if (webhookEndpoint.Status != WebhookEndpointStatus.Active)
        {
            throw new InvalidOperationException("Only an active endpoint can be snapshotted for local delivery.");
        }

        if (endpointConfigurationVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(endpointConfigurationVersion));
        }

        if (webhookEndpoint.SecretVersion < 1)
        {
            throw new InvalidOperationException("The endpoint signing credential version is invalid.");
        }

        if (webhookEndpoint.MaxAttempts < 1)
        {
            throw new InvalidOperationException("The endpoint must allow at least one delivery attempt.");
        }

        if (webhookEndpoint.TimeoutSeconds < 1)
        {
            throw new InvalidOperationException("The endpoint timeout must be positive.");
        }

        if (webhookEndpoint.RateLimitPerMinute is < 1)
        {
            throw new InvalidOperationException("The endpoint rate limit must be positive when configured.");
        }

        WebhookDeliveryPlanSnapshot.RequireTimestamp(capturedAtUtc, nameof(capturedAtUtc));
        WebhookDeliveryPlanSnapshot.RequireTimestamp(
            credentialValidFromUtc,
            nameof(credentialValidFromUtc));

        if (credentialValidUntilUtc is { } validUntilUtc)
        {
            WebhookDeliveryPlanSnapshot.RequireTimestamp(validUntilUtc, nameof(credentialValidUntilUtc));
            if (validUntilUtc <= credentialValidFromUtc)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(credentialValidUntilUtc),
                    "Credential validity must end after it begins.");
            }
        }

        return new WebhookLocalTargetSnapshot
        {
            Id = Guid.CreateVersion7(),
            TenantId = deliveryPlanSnapshot.TenantId,
            WebhookMessageId = deliveryPlanSnapshot.WebhookMessageId,
            DeliveryPlanSnapshotId = deliveryPlanSnapshot.Id,
            WebhookEndpointId = webhookEndpoint.Id,
            EndpointConfigurationVersion = endpointConfigurationVersion,
            DestinationUrl = NormalizeDestinationUrl(webhookEndpoint.Url),
            CredentialReference = WebhookDeliveryPlanSnapshot.NormalizeRequired(
                webhookEndpoint.SecretRef,
                MaxCredentialReferenceLength,
                nameof(webhookEndpoint.SecretRef)),
            CredentialVersion = webhookEndpoint.SecretVersion,
            CredentialValidFromUtc = credentialValidFromUtc,
            CredentialValidUntilUtc = credentialValidUntilUtc,
            MaxAttempts = webhookEndpoint.MaxAttempts,
            TimeoutSeconds = webhookEndpoint.TimeoutSeconds,
            RateLimitPerMinute = webhookEndpoint.RateLimitPerMinute,
            CapturedAtUtc = capturedAtUtc,
            DeliveryStatus = WebhookLocalDeliveryStatus.Pending,
            NextActionAtUtc = capturedAtUtc,
            ConcurrencyVersion = 1,
            CreatedAt = capturedAtUtc.UtcDateTime
        };
    }

    public void MigratePendingConfiguration(
        WebhookEndpoint webhookEndpoint,
        DateTimeOffset migratedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(webhookEndpoint);
        WebhookDeliveryPlanSnapshot.RequireTimestamp(migratedAtUtc, nameof(migratedAtUtc));
        if (DeliveryStatus != WebhookLocalDeliveryStatus.Pending ||
            ProcessingLeaseToken is not null ||
            ProcessingLeaseExpiresAtUtc is not null ||
            DeliveryFence != 0)
        {
            throw new InvalidOperationException(
                "Only unclaimed pending Local work can migrate to a new endpoint configuration.");
        }

        if (webhookEndpoint.Id != WebhookEndpointId || webhookEndpoint.TenantId != TenantId)
        {
            throw new InvalidOperationException(
                "The endpoint configuration must match the pending target tenant and endpoint.");
        }

        if (webhookEndpoint.ConfigurationVersion <= EndpointConfigurationVersion)
        {
            throw new InvalidOperationException(
                "The endpoint configuration migration must advance the snapshotted version.");
        }

        var credentialChanged = webhookEndpoint.SecretVersion != CredentialVersion ||
            !string.Equals(webhookEndpoint.SecretRef, CredentialReference, StringComparison.Ordinal);
        EndpointConfigurationVersion = webhookEndpoint.ConfigurationVersion;
        DestinationUrl = NormalizeDestinationUrl(webhookEndpoint.Url);
        CredentialReference = WebhookDeliveryPlanSnapshot.NormalizeRequired(
            webhookEndpoint.SecretRef,
            MaxCredentialReferenceLength,
            nameof(webhookEndpoint.SecretRef));
        CredentialVersion = webhookEndpoint.SecretVersion;
        if (credentialChanged)
        {
            if (webhookEndpoint.SecretActivatedAt == default ||
                webhookEndpoint.SecretActivatedAt.Kind != DateTimeKind.Utc)
            {
                throw new InvalidOperationException(
                    "Credential migration requires an authoritative UTC activation timestamp.");
            }

            CredentialValidFromUtc = new DateTimeOffset(webhookEndpoint.SecretActivatedAt);
            CredentialValidUntilUtc = null;
        }

        MaxAttempts = webhookEndpoint.MaxAttempts;
        TimeoutSeconds = webhookEndpoint.TimeoutSeconds;
        RateLimitPerMinute = webhookEndpoint.RateLimitPerMinute;
        CapturedAtUtc = migratedAtUtc;
        ConcurrencyVersion = checked(ConcurrencyVersion + 1);
        UpdatedAt = migratedAtUtc.UtcDateTime;
    }

    private static string NormalizeDestinationUrl(string destinationUrl)
    {
        var normalized = WebhookDeliveryPlanSnapshot.NormalizeRequired(
            destinationUrl,
            MaxDestinationUrlLength,
            nameof(destinationUrl));

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var destinationUri) ||
            (destinationUri.Scheme != Uri.UriSchemeHttp && destinationUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "Webhook destination must be an absolute HTTP or HTTPS URL.",
                nameof(destinationUrl));
        }

        return normalized;
    }
}
