// ABOUTME: Immutable instance-to-consumer application binding for one outgoing webhook provider.
// ABOUTME: Requires verified tenant ownership and governed typed capabilities before granting provider authority.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class WebhookConsumerProviderBinding : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; private set; } = null!;
    public Guid WebhookConsumerId { get; private set; }
    public WebhookConsumer WebhookConsumer { get; private set; } = null!;
    public Guid InstanceId { get; private set; }
    public WebhookProviderKind ProviderKind { get; private set; }
    public string ProviderVersion { get; private set; } = string.Empty;
    public string ProviderEnvironment { get; private set; } = string.Empty;
    public string ApplicationUid { get; private set; } = string.Empty;
    public string? ExternalApplicationId { get; private set; }
    public WebhookProviderBindingVerificationState VerificationState { get; private set; }
    public Guid? VerifiedTenantId { get; private set; }
    public Guid? VerifiedWebhookConsumerId { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public WebhookProviderCapability Capabilities { get; private set; }
    public WebhookProviderCapability GovernanceAllowedCapabilities { get; private set; }
    public string CapabilityResolutionVersion { get; private set; } = string.Empty;
    public DateTimeOffset CapabilitiesResolvedAtUtc { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public WebhookProviderCapability EffectiveGovernedCapabilities =>
        Capabilities & GovernanceAllowedCapabilities;

    public bool IsVerifiedFor(Guid tenantId, Guid webhookConsumerId) =>
        IsEnabled &&
        VerificationState == WebhookProviderBindingVerificationState.Verified &&
        TenantId == tenantId &&
        WebhookConsumerId == webhookConsumerId &&
        VerifiedTenantId == tenantId &&
        VerifiedWebhookConsumerId == webhookConsumerId &&
        !string.IsNullOrWhiteSpace(ExternalApplicationId) &&
        string.Equals(
            ApplicationUid,
            CreateApplicationUid(InstanceId, WebhookConsumerId),
            StringComparison.Ordinal);

    public bool CanIssueAppPortalFor(Guid tenantId, Guid webhookConsumerId) =>
        IsVerifiedFor(tenantId, webhookConsumerId) &&
        SupportsGoverned(WebhookProviderCapability.AppPortal);

    public bool SupportsGoverned(WebhookProviderCapability capability)
    {
        WebhookProviderCapabilityProfile.EnsureKnownCapabilities(capability, nameof(capability));
        return capability != WebhookProviderCapability.None &&
               (EffectiveGovernedCapabilities & capability) == capability;
    }

    public static WebhookConsumerProviderBinding CreatePending(
        Guid tenantId,
        Guid webhookConsumerId,
        Guid instanceId,
        string providerEnvironment,
        WebhookProviderCapabilityProfile capabilityProfile,
        WebhookProviderCapability governanceAllowedCapabilities)
    {
        return Create(
            tenantId,
            webhookConsumerId,
            instanceId,
            providerEnvironment,
            capabilityProfile,
            governanceAllowedCapabilities,
            WebhookProviderBindingVerificationState.Pending,
            externalApplicationId: null);
    }

    public static WebhookConsumerProviderBinding CreateLegacyUnverified(
        Guid tenantId,
        Guid webhookConsumerId,
        Guid instanceId,
        string providerEnvironment,
        string externalApplicationId,
        WebhookProviderCapabilityProfile capabilityProfile)
    {
        return Create(
            tenantId,
            webhookConsumerId,
            instanceId,
            providerEnvironment,
            capabilityProfile,
            WebhookProviderCapability.None,
            WebhookProviderBindingVerificationState.LegacyUnverified,
            WebhookProviderCapabilityProfile.NormalizeRequired(
                externalApplicationId,
                nameof(externalApplicationId)));
    }

    public void VerifyOwnership(
        Guid verifiedTenantId,
        Guid verifiedWebhookConsumerId,
        string externalApplicationId,
        DateTimeOffset verifiedAtUtc)
    {
        if (VerificationState is not WebhookProviderBindingVerificationState.Pending and
            not WebhookProviderBindingVerificationState.LegacyUnverified)
        {
            throw new InvalidOperationException(
                "Only pending or legacy-unverified provider bindings can be verified.");
        }

        if (verifiedTenantId != TenantId || verifiedWebhookConsumerId != WebhookConsumerId)
        {
            throw new InvalidOperationException(
                "Provider application ownership does not match the persisted tenant and webhook consumer.");
        }

        ExternalApplicationId = WebhookProviderCapabilityProfile.NormalizeRequired(
            externalApplicationId,
            nameof(externalApplicationId));
        VerifiedTenantId = verifiedTenantId;
        VerifiedWebhookConsumerId = verifiedWebhookConsumerId;
        VerifiedAtUtc = verifiedAtUtc;
        VerificationState = WebhookProviderBindingVerificationState.Verified;
        IsEnabled = true;
    }

    public void RejectVerification()
    {
        if (VerificationState is not WebhookProviderBindingVerificationState.Pending and
            not WebhookProviderBindingVerificationState.LegacyUnverified)
        {
            throw new InvalidOperationException(
                "Only pending or legacy-unverified provider bindings can be rejected.");
        }

        VerificationState = WebhookProviderBindingVerificationState.Rejected;
        IsEnabled = false;
    }

    public void Revoke()
    {
        if (VerificationState != WebhookProviderBindingVerificationState.Verified)
        {
            throw new InvalidOperationException("Only a verified provider binding can be revoked.");
        }

        VerificationState = WebhookProviderBindingVerificationState.Revoked;
        IsEnabled = false;
    }

    public void ReplaceCapabilityResolution(
        WebhookProviderCapabilityProfile capabilityProfile,
        WebhookProviderCapability governanceAllowedCapabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilityProfile);

        if (capabilityProfile.ProviderKind != ProviderKind ||
            !string.Equals(capabilityProfile.ProviderVersion, ProviderVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Capability resolution must target the binding's immutable provider kind and version.");
        }

        WebhookProviderCapabilityProfile.EnsureKnownCapabilities(
            governanceAllowedCapabilities,
            nameof(governanceAllowedCapabilities));

        Capabilities = capabilityProfile.Capabilities;
        GovernanceAllowedCapabilities = governanceAllowedCapabilities;
        CapabilityResolutionVersion = capabilityProfile.ResolutionVersion;
        CapabilitiesResolvedAtUtc = capabilityProfile.ResolvedAtUtc;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public void Enable()
    {
        if (VerificationState != WebhookProviderBindingVerificationState.Verified)
        {
            throw new InvalidOperationException("Only a verified provider binding can be enabled.");
        }

        IsEnabled = true;
    }

    public static string CreateApplicationUid(Guid instanceId, Guid webhookConsumerId)
    {
        EnsureRequired(instanceId, nameof(instanceId));
        EnsureRequired(webhookConsumerId, nameof(webhookConsumerId));
        return $"islamu-{instanceId:N}-consumer-{webhookConsumerId:N}";
    }

    private static WebhookConsumerProviderBinding Create(
        Guid tenantId,
        Guid webhookConsumerId,
        Guid instanceId,
        string providerEnvironment,
        WebhookProviderCapabilityProfile capabilityProfile,
        WebhookProviderCapability governanceAllowedCapabilities,
        WebhookProviderBindingVerificationState verificationState,
        string? externalApplicationId)
    {
        EnsureRequired(tenantId, nameof(tenantId));
        EnsureRequired(webhookConsumerId, nameof(webhookConsumerId));
        EnsureRequired(instanceId, nameof(instanceId));
        ArgumentNullException.ThrowIfNull(capabilityProfile);
        WebhookProviderCapabilityProfile.EnsureKnownCapabilities(
            governanceAllowedCapabilities,
            nameof(governanceAllowedCapabilities));

        return new WebhookConsumerProviderBinding
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            WebhookConsumerId = webhookConsumerId,
            InstanceId = instanceId,
            ProviderKind = capabilityProfile.ProviderKind,
            ProviderVersion = capabilityProfile.ProviderVersion,
            ProviderEnvironment = WebhookProviderCapabilityProfile.NormalizeRequired(
                providerEnvironment,
                nameof(providerEnvironment)),
            ApplicationUid = CreateApplicationUid(instanceId, webhookConsumerId),
            ExternalApplicationId = externalApplicationId,
            VerificationState = verificationState,
            Capabilities = capabilityProfile.Capabilities,
            GovernanceAllowedCapabilities = governanceAllowedCapabilities,
            CapabilityResolutionVersion = capabilityProfile.ResolutionVersion,
            CapabilitiesResolvedAtUtc = capabilityProfile.ResolvedAtUtc,
            IsEnabled = false
        };
    }

    private static void EnsureRequired(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }
}

public enum WebhookProviderBindingVerificationState
{
    LegacyUnverified = 1,
    Pending = 2,
    Verified = 3,
    Rejected = 4,
    Revoked = 5
}
