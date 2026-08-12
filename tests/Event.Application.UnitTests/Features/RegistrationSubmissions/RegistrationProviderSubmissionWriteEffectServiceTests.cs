// ABOUTME: Verifies the post-commit provider submission drain settles success, retry, and ambiguity independently.
// ABOUTME: Proves only mapped provider-transfer-approved canonical answers reach the provider sink.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Application.Services.Registration.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;

namespace Event.Application.UnitTests.Features.RegistrationSubmissions;

public sealed class RegistrationProviderSubmissionWriteEffectServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 18, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task AcceptedSinkCompletesEffectWithApprovedMappedAnswers()
    {
        EffectScope scope = CreateScope(SinkBehavior.Accept);

        int completed = await scope.Handler.Handle(
            new DrainRegistrationProviderSubmissionWriteEffectsCommand("worker"), CancellationToken.None);

        await Assert.That(completed).IsEqualTo(1);
        await scope.Repository.Received(1).CompleteAsync(scope.Claim, Now, Arg.Any<CancellationToken>());
        await Assert.That(scope.Sink.LastRequest!.Answers).IsEquivalentTo(
            new Dictionary<string, string> { ["email"] = "attendee@example.test" });
    }

    [Test]
    public async Task AcceptedSinkUsesCanonicalDotPlatformFieldKeys()
    {
        EffectScope scope = CreateScope(SinkBehavior.Accept);

        await scope.Handler.Handle(new DrainRegistrationProviderSubmissionWriteEffectsCommand("worker"), CancellationToken.None);

        await Assert.That(scope.Sink.LastRequest!.Answers.Keys).Contains("email");
    }

    [Test]
    public async Task EmptyResolvedAnswersWithTransferableMappingParksInsteadOfCompleting()
    {
        EffectScope scope = CreateScope(SinkBehavior.Accept, includeAnswers: false);

        int completed = await scope.Handler.Handle(
            new DrainRegistrationProviderSubmissionWriteEffectsCommand("worker"), CancellationToken.None);

        await Assert.That(completed).IsEqualTo(0);
        await scope.Repository.Received(1).ParkAmbiguousAsync(
            scope.Claim, "provider_submission_mapped_answers_empty", Now, Arg.Any<CancellationToken>());
        await scope.Repository.DidNotReceive().CompleteAsync(scope.Claim, Now, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryablePreHandoffFailureSchedulesRetryWithoutChangingSubmission()
    {
        EffectScope scope = CreateScope(SinkBehavior.Retryable);

        int completed = await scope.Handler.Handle(
            new DrainRegistrationProviderSubmissionWriteEffectsCommand("worker"), CancellationToken.None);

        await Assert.That(completed).IsEqualTo(0);
        await scope.Repository.Received(1).RetryAsync(
            scope.Claim, "provider_rate_limited", Arg.Any<DateTime>(), Now, Arg.Any<CancellationToken>());
        await scope.Repository.DidNotReceive().ParkAmbiguousAsync(
            Arg.Any<RegistrationProviderSubmissionWriteClaim>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await Assert.That(scope.Delivery.Submission.StatusId).IsEqualTo((int)RegistrationSubmissionStatusEnum.Received);
    }

    [Test]
    public async Task AmbiguousPostHandoffFailureParksWithoutRetrying()
    {
        EffectScope scope = CreateScope(SinkBehavior.Ambiguous);

        int completed = await scope.Handler.Handle(
            new DrainRegistrationProviderSubmissionWriteEffectsCommand("worker"), CancellationToken.None);

        await Assert.That(completed).IsEqualTo(0);
        await scope.Repository.Received(1).ParkAmbiguousAsync(
            scope.Claim, "provider_write_outcome_unknown", Now, Arg.Any<CancellationToken>());
        await scope.Repository.DidNotReceive().RetryAsync(
            Arg.Any<RegistrationProviderSubmissionWriteClaim>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await Assert.That(scope.Delivery.Submission.StatusId).IsEqualTo((int)RegistrationSubmissionStatusEnum.Received);
    }

    [Test]
    public async Task MirrorOnlyLaunchAcceptsSinkOnlyCapabilityAndRejectsWriteOnlyProviderApi()
    {
        var sink = new TestSink(SinkBehavior.Accept);
        MethodInfo isHeadlessBinding = typeof(LaunchNativeRegistrationAttemptCommandHandler).GetMethod(
            "IsHeadlessBinding",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        RegistrationProviderBinding mirror = HeadlessBinding(
            RegistrationProviderCollectionModeEnum.MirrorOnly,
            RegistrationProviderCapabilityCodes.SubmissionSink);
        RegistrationProviderBinding writeOnly = HeadlessBinding(
            RegistrationProviderCollectionModeEnum.ProviderApi,
            RegistrationProviderCapabilityCodes.SubmissionWrite);

        await Assert.That((bool)isHeadlessBinding.Invoke(null, [mirror, sink])!).IsTrue();
        await Assert.That((bool)isHeadlessBinding.Invoke(null, [writeOnly, sink])!).IsFalse();
    }

    private static EffectScope CreateScope(SinkBehavior behavior, bool includeAnswers = true)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            tenantId, "Formbricks", RegistrationProviderKindEnum.ExternalApi,
            RegistrationProviderDeploymentKindEnum.HostedSaas, "FORMBRICKS", "CLOUD", "v1",
            "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-10", "https://api.formbricks.example.test/api/v1",
            "https://forms.example.test", "workspace", Guid.CreateVersion7(), null, Now);
        RegistrationForm form = RegistrationForm.Create(tenantId, eventId, "profile", "main", "Profile", Now);
        RegistrationFormVersion version = RegistrationFormVersion.Create(form, 1, "en", null, null, Now);
        RegistrationFormSection section = RegistrationFormSection.Create(Guid.CreateVersion7(), version, 1, "Profile", Now);
        version.AddSection(section);
        RegistrationFormField field = RegistrationFormField.Create(
            Guid.CreateVersion7(), section, 1, "profile", "email", "Email",
            RegistrationFieldTypeEnum.ShortText, 1, RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers,
            false, true, Now);
        version.AddField(section, field);
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            tenantId, connection.Id, form.Id, version.Id, RegistrationProviderPresentationModeEnum.Embed,
            RegistrationProviderCollectionModeEnum.MirrorOnly, RegistrationProviderCompletionModeEnum.Callback,
            RegistrationProviderTrustLevelEnum.FullCanonical, null, Now);
        RegistrationProviderFieldMapping mapping = RegistrationProviderFieldMapping.Create(binding, "profile.email", "email", true);
        binding.AddFieldMapping(mapping);
        binding.AddCapability(RegistrationProviderCapability.Create(binding, "FORMBRICKS", "CLOUD", "v1",
            "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-10", RegistrationProviderCapabilityCodes.SubmissionWrite));
        binding.AddCapability(RegistrationProviderCapability.Create(binding, "FORMBRICKS", "CLOUD", "v1",
            "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-10", RegistrationProviderCapabilityCodes.SubmissionSink));
        binding.Publish(Evidence("mapping"), Now);
        SetPrivateProperty(binding, nameof(RegistrationProviderBinding.Connection), connection);
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(tenantId, eventId, "registration", Now);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            workflow, 1, RegistrationRequirementCriticalityEnum.Required, false,
            RegistrationRequirementCompletionEffectEnum.BlocksRegistration,
            RegistrationAnswerSyncModeEnum.FULL_CANONICAL,
            RegistrationRequirementSubjectTypeEnum.AllOrders, null, Now);
        RegistrationAttempt attempt = RegistrationAttempt.Create(
            tenantId, eventId, orderId, workflow.Id, requirement.Id, Guid.CreateVersion7(), form.Id, version.Id,
            CapabilityTokenHash.Create(Hash("capability")), binding.Id, binding.PublishedMappingRevisionHash,
            Now, Now.AddHours(1));
        RegistrationSubmission submission = attempt.SubmitHeadlessProvider(
            Evidence("answers"), Now, RegistrationTransportIdempotencyHash.Create(Hash("transport")));
        RegistrationAnswer answer = RegistrationAnswer.CreateText(
            submission, field, requirement, RegistrationAnswerSubjectTypeEnum.RegistrationOrder,
            orderId, 1, "attendee@example.test", Now);
        var delivery = new RegistrationProviderSubmissionWriteDelivery(attempt, submission, binding, includeAnswers ? [answer] : [], [field]);
        var claim = new RegistrationProviderSubmissionWriteClaim(
            Guid.CreateVersion7(), tenantId, submission.Id, attempt.Id, binding.Id,
            Guid.CreateVersion7(), 1, 1);
        IRegistrationProviderSubmissionWriteEffectRepository repository = Substitute.For<IRegistrationProviderSubmissionWriteEffectRepository>();
        repository.ClaimDueAsync("worker", 100, Now, TimeSpan.FromSeconds(60), Arg.Any<CancellationToken>())
            .Returns([claim]);
        repository.GetDeliveryAsync(claim, Arg.Any<CancellationToken>()).Returns(delivery);
        repository.CompleteAsync(claim, Now, Arg.Any<CancellationToken>()).Returns(true);
        var sink = new TestSink(behavior);
        ITenantContextAccessor tenantAccessor = Substitute.For<ITenantContextAccessor>();
        IRegistrationSensitiveValueProtector protector = Substitute.For<IRegistrationSensitiveValueProtector>();
        var handler = new DrainRegistrationProviderSubmissionWriteEffectsCommandHandler(
            repository, sink, tenantAccessor, protector, new FixedTimeProvider(Now));
        return new(handler, repository, sink, claim, delivery);
    }

    private static RegistrationProviderBinding HeadlessBinding(
        RegistrationProviderCollectionModeEnum collectionMode,
        string capabilityCode)
    {
        Guid tenantId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            tenantId, "Formbricks", RegistrationProviderKindEnum.ExternalApi,
            RegistrationProviderDeploymentKindEnum.HostedSaas, "FORMBRICKS", "CLOUD", "v1",
            "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-10", "https://api.formbricks.example.test/api/v1",
            "https://forms.example.test", "workspace", Guid.CreateVersion7(), null, Now);
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            tenantId, connection.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            RegistrationProviderPresentationModeEnum.Embed, collectionMode,
            RegistrationProviderCompletionModeEnum.Callback,
            RegistrationProviderTrustLevelEnum.SelectedFields, null, Now);
        binding.AddCapability(RegistrationProviderCapability.Create(
            binding, "FORMBRICKS", "CLOUD", "v1", "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-10", capabilityCode));
        binding.Publish(Evidence("mapping"), Now);
        SetPrivateProperty(binding, nameof(RegistrationProviderBinding.Connection), connection);
        return binding;
    }

    private static void SetPrivateProperty<T>(object target, string propertyName, T value) =>
        target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);

    private static RegistrationEvidenceHash Evidence(string value) => RegistrationEvidenceHash.Create(Hash(value));
    private static string Hash(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record EffectScope(
        DrainRegistrationProviderSubmissionWriteEffectsCommandHandler Handler,
        IRegistrationProviderSubmissionWriteEffectRepository Repository,
        TestSink Sink,
        RegistrationProviderSubmissionWriteClaim Claim,
        RegistrationProviderSubmissionWriteDelivery Delivery);

    private enum SinkBehavior { Accept, Retryable, Ambiguous }

    private sealed class TestSink(SinkBehavior behavior)
        : IRegistrationProviderRegistry, IRegistrationProviderDescriptor, IRegistrationProviderSubmissionSink
    {
        public RegistrationProviderTuple Tuple { get; } = new("FORMBRICKS", "CLOUD", "v1", "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-10");
        public RegistrationProviderCapabilitySet ProvenCapabilities { get; } = new(
            false, false, false, false, false, true, false, false, false, false, true, false);
        public RegistrationProviderSubmissionSinkRequest? LastRequest { get; private set; }
        public IRegistrationProviderDescriptor? TryResolve(RegistrationProviderTuple tuple) => tuple == Tuple ? this : null;

        public Task<RegistrationProviderSubmissionSinkResult> AcceptAsync(
            RegistrationProviderSubmissionSinkRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return behavior switch
            {
                SinkBehavior.Accept => Task.FromResult(new RegistrationProviderSubmissionSinkResult(true, Guid.CreateVersion7(), false)),
                SinkBehavior.Retryable => throw new RegistrationProviderSubmissionDeliveryException(
                    RegistrationProviderSubmissionDeliveryFailureKind.RetryableBeforeHandoff, "provider_rate_limited"),
                _ => throw new RegistrationProviderSubmissionDeliveryException(
                    RegistrationProviderSubmissionDeliveryFailureKind.AmbiguousAfterHandoff, "provider_write_outcome_unknown")
            };
        }
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
