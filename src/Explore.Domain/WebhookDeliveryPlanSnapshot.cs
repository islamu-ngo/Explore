// ABOUTME: Immutable tenant-scoped delivery-plan authority captured when an outgoing webhook message is materialized.
// ABOUTME: Freezes consumer mode, contract, configuration, and retention decisions so later settings cannot reroute queued work.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class WebhookDeliveryPlanSnapshot : ITenantEntity, IAuditableEntity
{
    public const int MaxVersionLength = 200;
    public const int MaxRetentionPolicyLength = 200;

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; private set; } = null!;
    public Guid WebhookMessageId { get; private set; }
    public WebhookMessage WebhookMessage { get; private set; } = null!;
    public Guid WebhookConsumerId { get; private set; }
    public WebhookConsumer WebhookConsumer { get; private set; } = null!;
    public WebhookProviderMode ProviderMode { get; private set; }
    public string ConfigurationVersion { get; private set; } = string.Empty;
    public string EventContractVersion { get; private set; } = string.Empty;
    public string RetentionPolicy { get; private set; } = string.Empty;
    public string RetentionPolicyVersion { get; private set; } = string.Empty;
    public DateTimeOffset PayloadRetentionUntilUtc { get; private set; }
    public DateTimeOffset MaterializedAtUtc { get; private set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static WebhookDeliveryPlanSnapshot Create(
        Guid tenantId,
        Guid webhookMessageId,
        Guid webhookConsumerId,
        WebhookProviderMode providerMode,
        string configurationVersion,
        string eventContractVersion,
        string retentionPolicy,
        string retentionPolicyVersion,
        DateTimeOffset payloadRetentionUntilUtc,
        DateTimeOffset materializedAtUtc)
    {
        RequireGuid(tenantId, nameof(tenantId));
        RequireGuid(webhookMessageId, nameof(webhookMessageId));
        RequireGuid(webhookConsumerId, nameof(webhookConsumerId));

        if (!Enum.IsDefined(providerMode))
        {
            throw new ArgumentOutOfRangeException(nameof(providerMode));
        }

        RequireTimestamp(materializedAtUtc, nameof(materializedAtUtc));
        RequireTimestamp(payloadRetentionUntilUtc, nameof(payloadRetentionUntilUtc));
        if (payloadRetentionUntilUtc < materializedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadRetentionUntilUtc),
                "Payload retention cannot end before the delivery plan is materialized.");
        }

        return new WebhookDeliveryPlanSnapshot
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            WebhookMessageId = webhookMessageId,
            WebhookConsumerId = webhookConsumerId,
            ProviderMode = providerMode,
            ConfigurationVersion = NormalizeRequired(
                configurationVersion,
                MaxVersionLength,
                nameof(configurationVersion)),
            EventContractVersion = NormalizeRequired(
                eventContractVersion,
                MaxVersionLength,
                nameof(eventContractVersion)),
            RetentionPolicy = NormalizeRequired(
                retentionPolicy,
                MaxRetentionPolicyLength,
                nameof(retentionPolicy)),
            RetentionPolicyVersion = NormalizeRequired(
                retentionPolicyVersion,
                MaxVersionLength,
                nameof(retentionPolicyVersion)),
            PayloadRetentionUntilUtc = payloadRetentionUntilUtc,
            MaterializedAtUtc = materializedAtUtc,
            CreatedAt = materializedAtUtc.UtcDateTime
        };
    }

    internal static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    internal static void RequireGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    internal static void RequireTimestamp(DateTimeOffset value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException("Timestamp is required.", parameterName);
        }
    }
}
