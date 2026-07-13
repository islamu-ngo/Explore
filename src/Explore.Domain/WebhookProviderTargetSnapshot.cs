// ABOUTME: Immutable external-provider target captured for one outgoing webhook delivery plan.
// ABOUTME: Freezes verified binding, application, environment, configuration, and credential authority without storing secrets.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class WebhookProviderTargetSnapshot : ITenantEntity, IAuditableEntity
{
    public const int MaxProviderValueLength = 500;
    public const int MaxCredentialReferenceLength = 500;

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; private set; } = null!;
    public Guid WebhookMessageId { get; private set; }
    public WebhookMessage WebhookMessage { get; private set; } = null!;
    public Guid DeliveryPlanSnapshotId { get; private set; }
    public WebhookDeliveryPlanSnapshot DeliveryPlanSnapshot { get; private set; } = null!;
    public Guid ProviderBindingId { get; private set; }
    public WebhookConsumerProviderBinding ProviderBinding { get; private set; } = null!;
    public WebhookProviderKind ProviderKind { get; private set; }
    public string ProviderVersion { get; private set; } = string.Empty;
    public string ProviderEnvironment { get; private set; } = string.Empty;
    public string ApplicationUid { get; private set; } = string.Empty;
    public string ExternalApplicationId { get; private set; } = string.Empty;
    public DateTimeOffset BindingVerifiedAtUtc { get; private set; }
    public string ProviderConfigurationVersion { get; private set; } = string.Empty;
    public string CredentialReference { get; private set; } = string.Empty;
    public int CredentialVersion { get; private set; }
    public DateTimeOffset CredentialValidFromUtc { get; private set; }
    public DateTimeOffset? CredentialValidUntilUtc { get; private set; }
    public DateTimeOffset CapturedAtUtc { get; private set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static WebhookProviderTargetSnapshot Create(
        WebhookDeliveryPlanSnapshot deliveryPlanSnapshot,
        WebhookConsumerProviderBinding providerBinding,
        string providerConfigurationVersion,
        string credentialReference,
        int credentialVersion,
        DateTimeOffset credentialValidFromUtc,
        DateTimeOffset? credentialValidUntilUtc,
        DateTimeOffset capturedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(deliveryPlanSnapshot);
        ArgumentNullException.ThrowIfNull(providerBinding);

        if (deliveryPlanSnapshot.ProviderMode is not (WebhookProviderMode.Svix or WebhookProviderMode.Composite))
        {
            throw new InvalidOperationException(
                "An external provider target requires a Svix or Composite delivery plan.");
        }

        if (!providerBinding.IsVerifiedFor(
                deliveryPlanSnapshot.TenantId,
                deliveryPlanSnapshot.WebhookConsumerId))
        {
            throw new InvalidOperationException(
                "The provider binding must be enabled and verified for the delivery plan tenant and consumer.");
        }

        if (providerBinding.ProviderKind == WebhookProviderKind.Local)
        {
            throw new InvalidOperationException("Local delivery must use a local target snapshot.");
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

        if (credentialVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(credentialVersion));
        }

        if (providerBinding.VerifiedAtUtc is not { } bindingVerifiedAtUtc ||
            string.IsNullOrWhiteSpace(providerBinding.ExternalApplicationId))
        {
            throw new InvalidOperationException(
                "The verified provider binding is missing immutable verification evidence.");
        }

        return new WebhookProviderTargetSnapshot
        {
            Id = Guid.CreateVersion7(),
            TenantId = deliveryPlanSnapshot.TenantId,
            WebhookMessageId = deliveryPlanSnapshot.WebhookMessageId,
            DeliveryPlanSnapshotId = deliveryPlanSnapshot.Id,
            ProviderBindingId = providerBinding.Id,
            ProviderKind = providerBinding.ProviderKind,
            ProviderVersion = WebhookDeliveryPlanSnapshot.NormalizeRequired(
                providerBinding.ProviderVersion,
                MaxProviderValueLength,
                nameof(providerBinding.ProviderVersion)),
            ProviderEnvironment = WebhookDeliveryPlanSnapshot.NormalizeRequired(
                providerBinding.ProviderEnvironment,
                MaxProviderValueLength,
                nameof(providerBinding.ProviderEnvironment)),
            ApplicationUid = WebhookDeliveryPlanSnapshot.NormalizeRequired(
                providerBinding.ApplicationUid,
                MaxProviderValueLength,
                nameof(providerBinding.ApplicationUid)),
            ExternalApplicationId = WebhookDeliveryPlanSnapshot.NormalizeRequired(
                providerBinding.ExternalApplicationId,
                MaxProviderValueLength,
                nameof(providerBinding.ExternalApplicationId)),
            BindingVerifiedAtUtc = bindingVerifiedAtUtc,
            ProviderConfigurationVersion = WebhookDeliveryPlanSnapshot.NormalizeRequired(
                providerConfigurationVersion,
                WebhookDeliveryPlanSnapshot.MaxVersionLength,
                nameof(providerConfigurationVersion)),
            CredentialReference = WebhookDeliveryPlanSnapshot.NormalizeRequired(
                credentialReference,
                MaxCredentialReferenceLength,
                nameof(credentialReference)),
            CredentialVersion = credentialVersion,
            CredentialValidFromUtc = credentialValidFromUtc,
            CredentialValidUntilUtc = credentialValidUntilUtc,
            CapturedAtUtc = capturedAtUtc,
            CreatedAt = capturedAtUtc.UtcDateTime
        };
    }
}
