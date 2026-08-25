// ABOUTME: Verifies managed provider publication checkpoints remote identities before local publication.
// ABOUTME: Proves incompatible forms cause no remote writes and webhook secrets are persisted encrypted.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using NSubstitute;
using System.Reflection;

namespace Event.Application.UnitTests.Features.RegistrationProviders;

public sealed class RegistrationProviderManagedPublishPreflightServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task IncompatibleForm_BlocksBeforeRemoteWrites()
    {
        TestScope scope = Scope();
        ManagedDescriptor descriptor = new(new("fingerprint", [new("registration_provider_conditions_unsupported", "Unsupported")]), "fingerprint");
        RegistrationProviderManagedPublishPreflightService service = Service(scope, descriptor, out IRegistrationProviderRepository providerRepository, out _);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_conditions_unsupported");
        await Assert.That(descriptor.ProvisionCalls).IsEqualTo(0);
        await Assert.That(descriptor.SubscriptionCalls).IsEqualTo(0);
        await providerRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CompatibleForm_CheckpointsSurveyWebhookSecretAndFingerprint()
    {
        TestScope scope = Scope();
        ManagedDescriptor descriptor = new(new("fingerprint", []), "fingerprint", SubscriptionCapabilities);
        RegistrationProviderManagedPublishPreflightService service = Service(scope, descriptor, out IRegistrationProviderRepository providerRepository, out ISecretBindingRepository secretRepository);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(scope.Binding.ProviderSurveyId).IsEqualTo("survey-managed");
        await Assert.That(scope.Binding.ProviderWebhookId).IsEqualTo("webhook-managed");
        await Assert.That(scope.Binding.WebhookSecretBindingId).IsNotNull();
        await providerRepository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        await secretRepository.Received(1).Create(Arg.Is<SecretBinding>(binding =>
            binding.SettingKey == SecretDefinitionRegistry.Keys.RegistrationProviders.WebhookSecret &&
            binding.ScopeId == scope.Binding.TenantId &&
            binding.InlineCiphertext != null));
    }

    [Test]
    public async Task SubscriptionCapableProvider_WithProviderAuthenticatedPushCreatesStateWithoutWebhookSecret()
    {
        TestScope scope = Scope();
        ManagedDescriptor descriptor = new(new("fingerprint", []), "fingerprint", SubscriptionCapabilities, providerWebhookSecret: null);
        RegistrationProviderManagedPublishPreflightService service = Service(scope, descriptor, out _, out ISecretBindingRepository secretRepository, out IRegistrationProviderSubscriptionStateRepository states);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(scope.Binding.ProviderWebhookId).IsEqualTo("webhook-managed");
        await Assert.That(scope.Binding.WebhookSecretBindingId).IsNull();
        await secretRepository.DidNotReceive().Create(Arg.Any<SecretBinding>());
        await states.Received(1).AddAsync(Arg.Is<RegistrationProviderSubscriptionState>(state =>
            state.TenantId == scope.Binding.TenantId &&
            state.RegistrationProviderBindingId == scope.Binding.Id &&
            state.WatchId == "webhook-managed" &&
            state.ProviderEventType == "RESPONSES" &&
            (state.PendingNotificationAt == Now || state.NextSweepAttemptAt <= Now)), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GoogleDelegatedManagedProvider_UsesManagedProvisionPathInsteadOfConnectorPreflight()
    {
        TestScope scope = Scope();
        scope.Binding.AddFieldMapping(RegistrationProviderFieldMapping.Create(scope.Binding, "system.registration_attempt_token", "entry.123456", true));
        GoogleManagedDelegatedDescriptor descriptor = new();
        RegistrationProviderManagedPublishPreflightService service = Service(scope, descriptor, out _, out _);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(scope.Binding.ProviderSurveyId).IsEqualTo("survey-managed");
        await Assert.That(scope.Binding.ProviderWebhookId).IsEqualTo("webhook-managed");
        await Assert.That(descriptor.ProvisionCalls).IsEqualTo(1);
        await Assert.That(descriptor.SubscriptionCalls).IsEqualTo(1);
    }

    [Test]
    [Arguments(null)]
    [Arguments("profile.email")]
    public async Task GoogleDelegatedManagedProvider_RequiresExactlyOneEntryCorrelationMapping(string? providerFieldKey)
    {
        TestScope scope = Scope();
        if (providerFieldKey is not null)
        {
            scope.Binding.AddFieldMapping(RegistrationProviderFieldMapping.Create(scope.Binding, "system.registration_attempt_token", providerFieldKey, true));
        }
        RegistrationProviderManagedPublishPreflightService service = Service(scope, new GoogleManagedDelegatedDescriptor(), out _, out _);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_correlation_mapping_invalid");
    }

    [Test]
    public async Task GoogleDelegatedManagedProvider_RejectsDuplicateCorrelationMappings()
    {
        TestScope scope = Scope();
        scope.Binding.AddFieldMapping(RegistrationProviderFieldMapping.Create(scope.Binding, "system.registration_attempt_token", "entry.123456", true));
        AddFieldMappingUnsafe(scope.Binding, RegistrationProviderFieldMapping.Create(scope.Binding, "system.registration_attempt_token", "entry.789", true));
        RegistrationProviderManagedPublishPreflightService service = Service(scope, new GoogleManagedDelegatedDescriptor(), out _, out _);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_correlation_mapping_invalid");
    }

    [Test]
    public async Task SubscriptionCapableProvider_WithExistingWebhookRequiresWebhookSecret()
    {
        TestScope scope = Scope();
        scope.Binding.SetDraftProvisionedSurvey("survey-managed", "revision-managed");
        scope.Binding.SetDraftProvisionedSubscription("webhook-managed", Guid.CreateVersion7());
        ManagedDescriptor descriptor = new(new("fingerprint", []), "fingerprint", SubscriptionCapabilities);
        RegistrationProviderManagedPublishPreflightService service = Service(scope, descriptor, out _, out _);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_webhook_missing");
        await Assert.That(descriptor.SubscriptionCalls).IsEqualTo(0);
    }

    [Test]
    public async Task ProviderWithoutSubscriptionCapability_DoesNotProvisionWebhookOrRequireSecret()
    {
        TestScope scope = Scope();
        ManagedDescriptor descriptor = new(new("fingerprint", []), "fingerprint", GoogleTask12Point2Capabilities);
        RegistrationProviderManagedPublishPreflightService service = Service(scope, descriptor, out IRegistrationProviderRepository providerRepository, out ISecretBindingRepository secretRepository);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(scope.Binding.ProviderSurveyId).IsEqualTo("survey-managed");
        await Assert.That(scope.Binding.ProviderWebhookId).IsNull();
        await Assert.That(scope.Binding.WebhookSecretBindingId).IsNull();
        await Assert.That(descriptor.SubscriptionCalls).IsEqualTo(0);
        await providerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await secretRepository.DidNotReceive().Create(Arg.Any<SecretBinding>());
    }

    [Test]
    public async Task DelegatedAutomation_RequiresCompleteMappingsSecretAndVerifiedTestCallback()
    {
        TestScope scope = Scope(RegistrationProviderTrustLevelEnum.CompletionOnly);
        Guid secretId = Guid.CreateVersion7();
        scope.Binding.SetDraftProvisionedSurvey("form-123", null);
        scope.Binding.SetDraftProvisionedSubscription("POWER_AUTOMATE_V1", secretId);
        scope.Binding.AddFieldMapping(RegistrationProviderFieldMapping.Create(scope.Binding, "profile.email", "question-email", true));
        scope.Binding.AddFieldMapping(RegistrationProviderFieldMapping.Create(scope.Binding, "system.registration_attempt_token", "question-attempt", true));
        DelegatedDescriptor descriptor = new();
        RegistrationProviderManagedPublishPreflightService service = Service(scope, descriptor, out IRegistrationProviderRepository providers, out ISecretBindingRepository secrets);
        SecretBinding secret = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.RegistrationProviders.WebhookSecret, SecretScope.Tenant, scope.Binding.TenantId,
            "MICROSOFT_FORMS_CALLBACK_KEY", qualifier: scope.Binding.Id.ToString("N"));
        secret.Id = secretId;
        secrets.GetByTenantAndIdAsync(scope.Binding.TenantId, secretId, Arg.Any<CancellationToken>()).Returns(secret);
        providers.GetLastCallbackAtAsync(scope.Binding.TenantId, scope.Binding.Id, Arg.Any<CancellationToken>()).Returns(Now);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task DelegatedAutomation_WithoutVerifiedTestCallbackFailsClosed()
    {
        TestScope scope = Scope(RegistrationProviderTrustLevelEnum.CompletionOnly);
        Guid secretId = Guid.CreateVersion7();
        scope.Binding.SetDraftProvisionedSurvey("form-123", null);
        scope.Binding.SetDraftProvisionedSubscription("POWER_AUTOMATE_V1", secretId);
        scope.Binding.AddFieldMapping(RegistrationProviderFieldMapping.Create(scope.Binding, "profile.email", "question-email", true));
        scope.Binding.AddFieldMapping(RegistrationProviderFieldMapping.Create(scope.Binding, "system.registration_attempt_token", "question-attempt", true));
        RegistrationProviderManagedPublishPreflightService service = Service(scope, new DelegatedDescriptor(), out _, out ISecretBindingRepository secrets);
        SecretBinding secret = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.RegistrationProviders.WebhookSecret, SecretScope.Tenant, scope.Binding.TenantId,
            "MICROSOFT_FORMS_CALLBACK_KEY", qualifier: scope.Binding.Id.ToString("N"));
        secret.Id = secretId;
        secrets.GetByTenantAndIdAsync(scope.Binding.TenantId, secretId, Arg.Any<CancellationToken>()).Returns(secret);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_test_callback_required");
    }

    [Test]
    public async Task SubscriptionCapableProvider_WithExistingWebhookRequiresBindingQualifiedSecret()
    {
        TestScope scope = Scope();
        Guid secretId = Guid.CreateVersion7();
        scope.Binding.SetDraftProvisionedSurvey("survey-managed", "revision-managed");
        scope.Binding.SetDraftProvisionedSubscription("webhook-managed", secretId);
        ManagedDescriptor descriptor = new(new("fingerprint", []), "fingerprint", SubscriptionCapabilities);
        RegistrationProviderManagedPublishPreflightService service = Service(scope, descriptor, out _, out ISecretBindingRepository secrets);
        SecretBinding wrongQualifier = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.RegistrationProviders.WebhookSecret,
            SecretScope.Tenant,
            scope.Binding.TenantId,
            "WEBHOOK_SECRET",
            qualifier: "other-binding");
        wrongQualifier.Id = secretId;
        secrets.GetByTenantAndIdAsync(scope.Binding.TenantId, secretId, Arg.Any<CancellationToken>()).Returns(wrongQualifier);

        RegistrationProviderManagedPublishPreflightResult result = await service.RunAsync(
            scope.Binding.TenantId, scope.EventId, scope.Binding, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_webhook_missing");
        await Assert.That(descriptor.SubscriptionCalls).IsEqualTo(0);
    }

    private static RegistrationProviderManagedPublishPreflightService Service(
        TestScope scope,
        IRegistrationProviderDescriptor descriptor,
        out IRegistrationProviderRepository providerRepository,
        out ISecretBindingRepository secretRepository)
        => Service(scope, descriptor, out providerRepository, out secretRepository, out _);

    private static RegistrationProviderManagedPublishPreflightService Service(
        TestScope scope,
        IRegistrationProviderDescriptor descriptor,
        out IRegistrationProviderRepository providerRepository,
        out ISecretBindingRepository secretRepository,
        out IRegistrationProviderSubscriptionStateRepository stateRepository)
    {
        IRegistrationFormAuthoringRepository forms = Substitute.For<IRegistrationFormAuthoringRepository>();
        forms.GetVersionAsync(scope.EventId, scope.Form.Id, scope.Version.Id, Arg.Any<CancellationToken>()).Returns(scope.Version);
        providerRepository = Substitute.For<IRegistrationProviderRepository>();
        IRegistrationProviderRegistry registry = Substitute.For<IRegistrationProviderRegistry>();
        registry.TryResolve(Arg.Any<RegistrationProviderTuple>()).Returns(descriptor);
        IRegistrationProviderCallbackUriBuilder callbackUris = Substitute.For<IRegistrationProviderCallbackUriBuilder>();
        callbackUris.Build(Arg.Any<string>(), scope.Binding.Id).Returns(new Uri("https://event.example.test/callback"));
        IInlineSecretProtector protector = Substitute.For<IInlineSecretProtector>();
        protector.Protect("whsec_test").Returns(new InlineProtectedSecret(new byte[] { 1, 2, 3 }, 1));
        secretRepository = Substitute.For<ISecretBindingRepository>();
        secretRepository.Create(Arg.Any<SecretBinding>()).Returns(call =>
        {
            SecretBinding binding = call.Arg<SecretBinding>();
            binding.Id = Guid.CreateVersion7();
            return binding;
        });
        stateRepository = Substitute.For<IRegistrationProviderSubscriptionStateRepository>();
        return new(forms, providerRepository, registry, callbackUris, protector, secretRepository, stateRepository, new FixedTimeProvider(Now));
    }

    private static TestScope Scope(RegistrationProviderTrustLevelEnum trustLevel = RegistrationProviderTrustLevelEnum.FullCanonical)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        RegistrationForm form = RegistrationForm.Create(Guid.CreateVersion7(), tenantId, eventId, "registration", "managed", "Managed", Now);
        RegistrationFormVersion version = RegistrationFormVersion.Create(Guid.CreateVersion7(), form, 1, "en", null, null, Now);
        RegistrationFormSection section = RegistrationFormSection.Create(Guid.CreateVersion7(), version, 1, "Profile", Now);
        version.AddSection(section);
        RegistrationFormField field = RegistrationFormField.Create(
            Guid.CreateVersion7(), section, 1, "profile", "email", "Email", RegistrationFieldTypeEnum.Email, 1,
            RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers, false, true, Now);
        version.AddField(section, field);
        version.UpdateFieldValidation(field, true, false, null, null, null, null, null, null, null, null);
        form.AddVersion(version);
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            Guid.CreateVersion7(), tenantId, "Formbricks", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.HostedSaas, "FORMBRICKS", "CLOUD", "v1",
            "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-10", "https://api.formbricks.test/api/v1",
            "https://forms.formbricks.test", "workspace", Guid.CreateVersion7(), Guid.CreateVersion7(), Now);
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            tenantId, connection.Id, form.Id, version.Id, RegistrationProviderPresentationModeEnum.Embed,
            RegistrationProviderCollectionModeEnum.ProviderHosted, RegistrationProviderCompletionModeEnum.Callback,
            trustLevel, null, Now);
        typeof(RegistrationProviderBinding).GetProperty(nameof(RegistrationProviderBinding.Connection))!.SetValue(binding, connection);
        return new(eventId, form, version, binding);
    }

    private sealed record TestScope(Guid EventId, RegistrationForm Form, RegistrationFormVersion Version, RegistrationProviderBinding Binding);

    private static void AddFieldMappingUnsafe(RegistrationProviderBinding binding, RegistrationProviderFieldMapping mapping)
    {
        FieldInfo field = typeof(RegistrationProviderBinding).GetField("_fieldMappings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((List<RegistrationProviderFieldMapping>)field.GetValue(binding)!).Add(mapping);
    }

    private static RegistrationProviderCapabilitySet SubscriptionCapabilities { get; } = new(
        Redirect: true,
        Embed: true,
        Manual: true,
        SchemaRead: true,
        FormProvision: true,
        SubmissionWrite: true,
        SubmissionRead: true,
        CallbackVerification: true,
        SubscriptionManagement: true,
        Reconciliation: true,
        SubmissionSink: true,
        AutoFinalize: true);

    private static RegistrationProviderCapabilitySet GoogleTask12Point2Capabilities { get; } = new(
        Redirect: true,
        Embed: true,
        Manual: true,
        SchemaRead: true,
        FormProvision: true,
        SubmissionWrite: false,
        SubmissionRead: true,
        CallbackVerification: false,
        SubscriptionManagement: false,
        Reconciliation: false,
        SubmissionSink: false,
        AutoFinalize: false);

    private class ManagedDescriptor(
        RegistrationProviderFormCompatibilityResult compatibility,
        string remoteFingerprint,
        RegistrationProviderCapabilitySet? provenCapabilities = null,
        string? providerWebhookSecret = "whsec_test") : IRegistrationProviderDescriptor, IRegistrationProviderFormCompatibilityChecker,
        IRegistrationProviderFormProvisioner, IRegistrationProviderSchemaReader, IRegistrationProviderSubscriptionManager
    {
        public RegistrationProviderTuple Tuple { get; } = new("FORMBRICKS", "CLOUD", "v1", "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-10");
        public RegistrationProviderCapabilitySet ProvenCapabilities { get; } = provenCapabilities ?? RegistrationProviderCapabilitySet.None;
        public int ProvisionCalls { get; private set; }
        public int SubscriptionCalls { get; private set; }

        public RegistrationProviderFormCompatibilityResult CheckCompatibility(RegistrationFormVersion formVersion) => compatibility;

        public Task<RegistrationProviderFormProvisionResult> ProvisionFormAsync(RegistrationProviderFormProvisionRequest request, CancellationToken cancellationToken)
        {
            ProvisionCalls++;
            return Task.FromResult(new RegistrationProviderFormProvisionResult("survey-managed", "revision-managed"));
        }

        public Task<RegistrationProviderSchemaReadResult> ReadSchemaAsync(RegistrationProviderSchemaReadRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new RegistrationProviderSchemaReadResult(new([]), true, remoteFingerprint));

        public Task<RegistrationProviderSubscriptionResult> EnsureSubscriptionAsync(RegistrationProviderSubscriptionRequest request, CancellationToken cancellationToken)
        {
            SubscriptionCalls++;
            return Task.FromResult(new RegistrationProviderSubscriptionResult(true, "webhook-managed", providerWebhookSecret, Now.AddDays(7)));
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class DelegatedDescriptor : IRegistrationProviderDescriptor, IRegistrationProviderDelegatedAutomation
    {
        public RegistrationProviderTuple Tuple { get; } = new("MICROSOFT_FORMS", "MICROSOFT_365", "POWER_AUTOMATE_V1", "ISLAMU_EVENT_MICROSOFT_FORMS_V1", "2026-08-11");
        public RegistrationProviderCapabilitySet ProvenCapabilities => RegistrationProviderCapabilitySet.None;
        public string ConnectorContractVersion => "POWER_AUTOMATE_V1";
        public string RequiredCorrelationPlatformFieldKey => "system.registration_attempt_token";
    }

    private sealed class GoogleManagedDelegatedDescriptor : ManagedDescriptor, IRegistrationProviderDelegatedAutomation
    {
        public GoogleManagedDelegatedDescriptor()
            : base(new("fingerprint", []), "fingerprint", SubscriptionCapabilities, providerWebhookSecret: null)
        {
        }

        public string ConnectorContractVersion => "GOOGLE_FORMS_ENTRY_CORRELATION_V1";
        public string RequiredCorrelationPlatformFieldKey => "system.registration_attempt_token";
    }
}
