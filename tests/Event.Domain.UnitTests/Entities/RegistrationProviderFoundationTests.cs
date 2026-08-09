// ABOUTME: Covers Phase 9 provider-neutral domain guards for credentials, channel shape, and immutable mappings.
// ABOUTME: Keeps provider configuration tests in Domain with no adapter or persistence dependency.

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;

namespace Event.Domain.UnitTests.Entities;

public sealed class RegistrationProviderFoundationTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task SecretBinding_DefaultQualifierPreservesUnqualifiedIdentity()
    {
        SecretBinding binding = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.Smtp.Password,
            SecretScope.Tenant,
            Guid.CreateVersion7(),
            "SMTP_PASSWORD");

        await Assert.That(binding.Qualifier).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task SecretBinding_ProviderQualifierIsTrimmedAndBounded()
    {
        Guid tenantId = Guid.CreateVersion7();
        SecretBinding binding = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.RegistrationProviders.ApiToken,
            SecretScope.Tenant,
            tenantId,
            "REGISTRATION_PROVIDER_API_TOKEN",
            qualifier: " connection-a ");

        await Assert.That(binding.Qualifier).IsEqualTo("connection-a");
        await Assert.That(() => SecretBinding.CreateEnvironmentVariable(
                SecretDefinitionRegistry.Keys.RegistrationProviders.ApiToken,
                SecretScope.Tenant,
                tenantId,
                "REGISTRATION_PROVIDER_API_TOKEN",
                qualifier: new string('x', 129)))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task RegistrationChannel_NativeAndProviderShapesAreExclusive()
    {
        RegistrationRequirement requirement = Requirement();
        await Assert.That(RegistrationChannel.Create(requirement, 1, true, null, Now).IsNative).IsTrue();
        await Assert.That(RegistrationChannel.Create(requirement, 2, false, Guid.CreateVersion7(), Now).RegistrationProviderBindingId).IsNotNull();
        await Assert.That(() => RegistrationChannel.Create(requirement, 3, true, Guid.CreateVersion7(), Now)).Throws<ArgumentException>();
        await Assert.That(() => RegistrationChannel.Create(requirement, 4, false, null, Now)).Throws<ArgumentException>();
    }

    [Test]
    public async Task PublishedProviderBindingRejectsMappingMutation()
    {
        RegistrationProviderBinding binding = Binding();
        RegistrationProviderFieldMapping mapping = RegistrationProviderFieldMapping.Create(binding, "attendee.email", "email", true);
        binding.AddFieldMapping(mapping);
        binding.Publish(Hash(), Now.AddMinutes(1));

        await Assert.That(binding.StateId).IsEqualTo((int)RegistrationProviderBindingStateEnum.Published);
        await Assert.That(() => binding.AddFieldMapping(RegistrationProviderFieldMapping.Create(binding, "attendee.name", "name", false)))
            .Throws<InvalidOperationException>();
        await Assert.That(binding.PublishedMappingRevisionHash).IsEqualTo(Hash());
    }

    [Test]
    public async Task PublishedProviderBindingRejectsDirectMappingFactories()
    {
        RegistrationProviderBinding binding = Binding();
        RegistrationProviderFieldMapping field = RegistrationProviderFieldMapping.Create(binding, "attendee.email", "email", true);
        binding.AddFieldMapping(field);
        binding.Publish(Hash(), Now.AddMinutes(1));

        await Assert.That(() => RegistrationProviderCapability.Create(binding, "forms", "hosted", "v1", "policy-1", "evidence-1", "callback"))
            .Throws<InvalidOperationException>();
        await Assert.That(() => RegistrationProviderFieldMapping.Create(binding, "attendee.name", "name", false))
            .Throws<InvalidOperationException>();
        await Assert.That(() => RegistrationProviderOptionMapping.Create(binding, field, "yes", "1"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task CapabilityTuplePersistsUnknownProviderIdentityShape()
    {
        RegistrationProviderBinding binding = Binding();
        RegistrationProviderCapability capability = RegistrationProviderCapability.Create(
            binding, "unknown-provider", "self-hosted", "2026-08", "policy-7", "rev-42", "schema.read");
        binding.AddCapability(capability);

        await Assert.That(capability.TupleKey).IsEqualTo("unknown-provider|self-hosted|2026-08|policy-7|rev-42|schema.read");
        await Assert.That(() => RegistrationProviderCapability.Create(binding, " ", "self-hosted", "v1", "p1", "r1", "schema.read"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Registry_DefinesTenantScopedRegistrationProviderCredentials()
    {
        SecretDefinition apiToken = SecretDefinitionRegistry.GetRequired(SecretDefinitionRegistry.Keys.RegistrationProviders.ApiToken);
        SecretDefinition webhookSecret = SecretDefinitionRegistry.GetRequired(SecretDefinitionRegistry.Keys.RegistrationProviders.WebhookSecret);

        await Assert.That(apiToken.AllowedScopes).IsEquivalentTo([SecretScope.Tenant]);
        await Assert.That(webhookSecret.AllowedScopes).IsEquivalentTo([SecretScope.Tenant]);
        await Assert.That(apiToken.AllowedSources.Contains(SecretSourceType.InlineEncrypted)).IsTrue();
        await Assert.That(webhookSecret.DefaultInfisicalPath).IsEqualTo("/registration-providers");
    }

    private static RegistrationProviderBinding Binding() => RegistrationProviderBinding.Create(
        Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
        RegistrationProviderPresentationModeEnum.Redirect, RegistrationProviderCollectionModeEnum.ProviderHosted,
        RegistrationProviderCompletionModeEnum.Callback, RegistrationProviderTrustLevelEnum.SelectedFields, Now);

    private static RegistrationRequirement Requirement()
    {
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "attendee", Now);
        return RegistrationRequirement.Create(workflow, 1, RegistrationRequirementCriticalityEnum.Optional, true,
            RegistrationRequirementCompletionEffectEnum.EnrichesRegistration, RegistrationAnswerSyncModeEnum.SELECTED_FIELDS,
            RegistrationRequirementSubjectTypeEnum.AllOrders, null, Now);
    }

    internal static RegistrationEvidenceHash Hash() => RegistrationEvidenceHash.Create(Convert.ToBase64String(new byte[32]));
}
