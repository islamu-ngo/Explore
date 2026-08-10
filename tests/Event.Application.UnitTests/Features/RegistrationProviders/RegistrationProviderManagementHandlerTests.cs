// ABOUTME: Covers registration-provider management fencing for retained effects, manual imports, and resolves.
// ABOUTME: Keeps queue redrive/ack tests in Application without Docker or API host dependencies.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationProviders;
using Explore.Application.Features.RegistrationProviders.Commands;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.RegistrationProviders;

public sealed class RegistrationProviderManagementHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task PublishEventBinding_ComputesCanonicalRevisionHashServerSide()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        var repository = new FakeProviderRepository(binding, eventId);
        var canonical = new PublishRegistrationProviderBindingCommandHandler(repository);
        IMediator mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<PublishRegistrationProviderBindingCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => canonical.Handle(call.Arg<PublishRegistrationProviderBindingCommand>(), call.Arg<CancellationToken>()));
        var handler = new PublishEventRegistrationProviderBindingCommandHandler(
            repository,
            mediator,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new PublishEventRegistrationProviderBindingCommand(binding.TenantId, eventId, binding.Id),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(binding.PublishedMappingRevisionHash).IsNotNull();
    }

    [Test]
    public async Task PollReconciliation_RejectsUtcDefaultCheckpointBeforeProviderDispatch()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        binding.AddCapability(RegistrationProviderCapability.Create(
            binding, "provider", "hosted", "v1", "policy", "evidence",
            RegistrationProviderCapabilityCodes.Reconciliation));
        var handler = new PollRegistrationProviderReconciliationCommandHandler(
            new FakeProviderRepository(binding, eventId),
            new Registry());

        BaseCommandResponse<Guid> result = await handler.Handle(
            new PollRegistrationProviderReconciliationCommand(
                binding.TenantId, eventId, binding.Id, DateTime.SpecifyKind(default, DateTimeKind.Utc)),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_reconciliation_validation_failed");
    }

    [Test]
    public async Task ConnectionList_UsesEventChannelAuthorityWhileMutationsRemainTenantScoped()
    {
        AuthorizeResourceAttribute attribute = typeof(GetRegistrationProviderConnectionsQuery)
            .GetCustomAttribute<AuthorizeResourceAttribute>()!;

        await Assert.That(attribute.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Events.ManageRegistrationChannels);
    }

    [Test]
    public async Task LaunchDescriptor_UsesEventChannelAuthority()
    {
        AuthorizeResourceAttribute attribute = typeof(GetRegistrationProviderLaunchDescriptorQuery)
            .GetCustomAttribute<AuthorizeResourceAttribute>()!;

        await Assert.That(attribute.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Events.ManageRegistrationChannels);
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(typeof(GetRegistrationProviderLaunchDescriptorQuery))).IsTrue();
    }

    [Test]
    public async Task ConnectionRequest_RequiresBothSecretBindings()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        var handler = new UpsertRegistrationProviderConnectionCommandHandler(
            new FakeProviderRepository(binding, eventId),
            new FixedTimeProvider(Now));
        var missing = new RegistrationProviderConnectionRequestDto
        {
            Name = "Provider",
            ProviderKindId = (int)RegistrationProviderKindEnum.ExternalForm,
            DeploymentKindId = (int)RegistrationProviderDeploymentKindEnum.SelfHosted
        };

        BaseCommandResponse<Guid> result = await handler.Handle(
            new UpsertRegistrationProviderConnectionCommand(binding.TenantId, eventId, null, missing),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_connection_validation_failed");
    }

    [Test]
    public async Task Retry_ValidatesGenerationAndEventBindingBeforeRedrive()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        IncomingWebhookEffectOutbox effect = DeadLetteredEffect(binding, out _);
        var providerRepository = new FakeProviderRepository(binding, eventId);
        var effectRepository = new FakeEffectRepository([effect]);
        var handler = new RetryRegistrationProviderParkedItemCommandHandler(
            providerRepository,
            effectRepository,
            new FakeReceiptRepository(),
            new FixedTimeProvider(Now.AddMinutes(1)));

        var denied = await handler.Handle(new RetryRegistrationProviderParkedItemCommand(
            binding.TenantId, Guid.CreateVersion7(), null, effect.Id, effect.ProcessingGeneration, "wrong-event"), CancellationToken.None);
        var accepted = await handler.Handle(new RetryRegistrationProviderParkedItemCommand(
            binding.TenantId, eventId, null, effect.Id, 1, "retry"), CancellationToken.None);

        await Assert.That(denied.Success).IsFalse();
        await Assert.That(denied.FailureCode).IsEqualTo("registration_provider_effect_not_found");
        await Assert.That(accepted.Success).IsTrue();
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.Pending);
        await Assert.That(effect.ProcessingGeneration).IsEqualTo(2);
    }

    [Test]
    public async Task Retry_RejectsRetainedEffectWhenReceiptIdentityConflicts()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        IncomingWebhookEffectOutbox effect = DeadLetteredEffect(binding, out IncomingWebhookMessage message);
        IncomingWebhookEffectReceipt wrongReceipt = IncomingWebhookEffectReceipt.Create(
            binding.TenantId,
            message.Id,
            ProcessProviderSubmissionEffectCommandHandler.StableEffectKind,
            "sha256:" + new string('1', 64),
            1,
            Now);
        var handler = new RetryRegistrationProviderParkedItemCommandHandler(
            new FakeProviderRepository(binding, eventId),
            new FakeEffectRepository([effect]),
            new FakeReceiptRepository(wrongReceipt),
            new FixedTimeProvider(Now.AddMinutes(1)));

        var result = await handler.Handle(new RetryRegistrationProviderParkedItemCommand(
            binding.TenantId, eventId, null, effect.Id, 1, "retry"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.DeadLettered);
    }

    [Test]
    public async Task ManualImport_IsIdempotentAndParksUnsupportedCapability()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding supported = Binding();
        supported.AddCapability(RegistrationProviderCapability.Create(supported, "provider", "hosted", "v1", "policy", "evidence", RegistrationProviderCapabilityCodes.Manual));
        RegistrationProviderBinding unsupported = Binding();
        var supportedMessages = new FakeMessageRepository();
        var supportedEffects = new FakeEffectRepository([]);
        var supportedHandler = new QueueManualRegistrationProviderImportCommandHandler(
            new FakeProviderRepository(supported, eventId), supportedMessages, supportedEffects, new FixedTimeProvider(Now));
        var unsupportedEffects = new FakeEffectRepository([]);
        var unsupportedHandler = new QueueManualRegistrationProviderImportCommandHandler(
            new FakeProviderRepository(unsupported, eventId), new FakeMessageRepository(), unsupportedEffects, new FixedTimeProvider(Now));

        var first = await supportedHandler.Handle(new(supported.TenantId, eventId, supported.Id, "storage:object/123", "operator-import-1"), CancellationToken.None);
        var second = await supportedHandler.Handle(new(supported.TenantId, eventId, supported.Id, "storage:object/123", "operator-import-1"), CancellationToken.None);
        var unsupportedResult = await unsupportedHandler.Handle(new(unsupported.TenantId, eventId, unsupported.Id, "storage:object/123", "operator-import-1"), CancellationToken.None);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(second.Success).IsTrue();
        await Assert.That(supportedMessages.Messages.Count).IsEqualTo(1);
        await Assert.That(supportedEffects.Effects.Count).IsEqualTo(1);
        await Assert.That(supportedEffects.Effects.Single().Status).IsEqualTo(OutboxMessageStatus.Completed);
        await Assert.That(supportedEffects.Effects.Single().EffectKind).IsEqualTo(QueueManualRegistrationProviderImportCommandHandler.ManualImportEffectKind);
        await Assert.That(unsupportedResult.Success).IsTrue();
        await Assert.That(unsupportedEffects.Effects.Single().EffectKind).IsEqualTo(QueueManualRegistrationProviderImportCommandHandler.ManualImportEffectKind);
        await Assert.That(unsupportedEffects.Effects.Single().Status).IsEqualTo(OutboxMessageStatus.DeadLettered);
        await Assert.That(unsupportedEffects.Effects.Single().FailureCategory).IsEqualTo("manual_import_unsupported");
    }

    [Test]
    public async Task Resolve_LoadsExactRetainedEffectAndDeniesCrossEventIds()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        IncomingWebhookEffectOutbox effect = DeadLetteredEffect(binding, out _);
        var handler = new ResolveRegistrationProviderQueueItemCommandHandler(
            new FakeProviderRepository(binding, eventId),
            new FakeEffectRepository([effect]),
            new FakeReceiptRepository(),
            new FixedTimeProvider(Now.AddMinutes(1)));

        var denied = await handler.Handle(new ResolveRegistrationProviderQueueItemCommand(
            binding.TenantId, Guid.CreateVersion7(), null, effect.Id, "accepted", "note:bounded"), CancellationToken.None);
        var accepted = await handler.Handle(new ResolveRegistrationProviderQueueItemCommand(
            binding.TenantId, eventId, null, effect.Id, "accepted", "note:bounded"), CancellationToken.None);

        await Assert.That(denied.Success).IsFalse();
        await Assert.That(accepted.Success).IsTrue();
        await Assert.That(effect.Status).IsEqualTo(OutboxMessageStatus.Completed);
        await Assert.That(effect.CompletedAt).IsEqualTo(Now.AddMinutes(1));
        await Assert.That(effect.FailureCategory).IsEqualTo("organizer_accepted");
        await Assert.That(effect.SafeDetail).DoesNotContain("note:bounded");
    }

    [Test]
    public async Task Resolve_SubmissionPersistsBoundedResolutionIssue()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        RegistrationSubmission submission = ProviderSubmission(binding, eventId);
        var providerRepository = new FakeProviderRepository(binding, eventId, submission);
        var handler = new ResolveRegistrationProviderQueueItemCommandHandler(
            providerRepository,
            new FakeEffectRepository([]),
            new FakeReceiptRepository(),
            new FixedTimeProvider(Now.AddMinutes(1)));

        var result = await handler.Handle(new ResolveRegistrationProviderQueueItemCommand(
            binding.TenantId, eventId, submission.Id, null, "accepted_with_operator_note", "note:bounded"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(providerRepository.Issues).HasSingleItem();
        await Assert.That(providerRepository.Issues.Single().Code).IsEqualTo("RESOLVED_ACCEPTED_WITH_OPERATOR_NOTE");
    }

    [Test]
    public async Task LaunchDescriptor_AllowsOnlyApprovedOriginAndNeverReturnsIframeHtml()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding(RegistrationProviderPresentationModeEnum.Embed);
        AddLaunchCapabilities(binding, RegistrationProviderCapabilityCodes.Embed);
        binding.Publish(Hash(), Now.AddMinutes(1));
        RegistrationRequirement requirement = RequirementWithChannel(binding, eventId, out RegistrationChannel channel);
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            binding.TenantId, "forms", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.HostedSaas, null, null, Now);
        connection.ReplaceApprovedOrigins(["https://forms.example.org"], Now);
        typeof(RegistrationProviderBinding).GetProperty(nameof(RegistrationProviderBinding.Connection))!.SetValue(binding, connection);
        var handler = new GetRegistrationProviderLaunchDescriptorQueryHandler(
            new FakeProviderRepository(binding, eventId, requirement: requirement),
            new Registry(new PresentationDescriptor(new("forms", "hosted", "v1", "policy", "evidence"), new Uri("https://forms.example.org/"))));

        RegistrationProviderLaunchDescriptorDto result = await handler.Handle(new(binding.TenantId, eventId, requirement.RegistrationWorkflowId, requirement.Id, channel.Id, binding.Id), CancellationToken.None);

        await Assert.That(result.Mode).IsEqualTo("embed");
        await Assert.That(result.ChannelId).IsEqualTo(channel.Id);
        await Assert.That(result.Url).IsEqualTo("https://forms.example.org/");
        await Assert.That(result.Url).DoesNotContain("<iframe");
    }

    [Test]
    public async Task LaunchDescriptor_RejectsCrossEventCrossBindingAndDeletedChannelWithoutUrlDisclosure()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding(RegistrationProviderPresentationModeEnum.Embed);
        AddLaunchCapabilities(binding, RegistrationProviderCapabilityCodes.Embed);
        binding.Publish(Hash(), Now.AddMinutes(1));
        RegistrationRequirement requirement = RequirementWithChannel(binding, eventId, out RegistrationChannel channel);
        RegistrationProviderBinding otherBinding = Binding(RegistrationProviderPresentationModeEnum.Embed);
        var handler = new GetRegistrationProviderLaunchDescriptorQueryHandler(
            new FakeProviderRepository(binding, eventId, requirement: requirement),
            new Registry(new PresentationDescriptor(new("forms", "hosted", "v1", "policy", "evidence"), new Uri("https://forms.example.org/"))));

        RegistrationProviderLaunchDescriptorDto crossEvent = await handler.Handle(new(binding.TenantId, Guid.CreateVersion7(), requirement.RegistrationWorkflowId, requirement.Id, channel.Id, binding.Id), CancellationToken.None);
        RegistrationProviderLaunchDescriptorDto crossBinding = await handler.Handle(new(binding.TenantId, eventId, requirement.RegistrationWorkflowId, requirement.Id, channel.Id, otherBinding.Id), CancellationToken.None);
        channel.Remove(Now.AddMinutes(2));
        RegistrationProviderLaunchDescriptorDto deleted = await handler.Handle(new(binding.TenantId, eventId, requirement.RegistrationWorkflowId, requirement.Id, channel.Id, binding.Id), CancellationToken.None);

        await Assert.That(crossEvent.Available).IsFalse();
        await Assert.That(crossBinding.Available).IsFalse();
        await Assert.That(deleted.Available).IsFalse();
        await Assert.That(crossEvent.Url).IsNull();
        await Assert.That(crossBinding.Url).IsNull();
        await Assert.That(deleted.Url).IsNull();
    }

    [Test]
    public async Task CreateBinding_RejectsCrossEventFormVersionLineage()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding existing = Binding();
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            existing.TenantId,
            "forms",
            RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.HostedSaas,
            null,
            null,
            Now);
        var handler = new CreateRegistrationProviderBindingCommandHandler(
            new FakeProviderRepository(existing, eventId, connection: connection),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreateRegistrationProviderBindingCommand(
            existing.TenantId,
            eventId,
            new RegistrationProviderBindingRequestDto
            {
                ConnectionId = connection.Id,
                FormId = Guid.CreateVersion7(),
                FormVersionId = Guid.CreateVersion7(),
                PresentationModeId = (int)RegistrationProviderPresentationModeEnum.Redirect,
                CollectionModeId = (int)RegistrationProviderCollectionModeEnum.ProviderHosted,
                CompletionModeId = (int)RegistrationProviderCompletionModeEnum.Callback,
                TrustLevelId = (int)RegistrationProviderTrustLevelEnum.FullCanonical
            }), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_binding_validation_failed");
    }

    [Test]
    public async Task ReplaceMappingsStoresStructuredFieldsOptionsAndProjectsForReload()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        var handler = new ReplaceDraftRegistrationProviderMappingsCommandHandler(new FakeProviderRepository(binding, eventId));

        BaseCommandResponse<Guid> result = await handler.Handle(new ReplaceDraftRegistrationProviderMappingsCommand(
            binding.TenantId,
            binding.Id,
            [new("attendee.email", "q_email", true), new("meal", "q_meal", false)],
            [new("meal", "vegetarian", "opt_1")]), CancellationToken.None);
        RegistrationProviderBindingDto dto = (await new GetRegistrationProviderBindingQueryHandler(new FakeProviderRepository(binding, eventId))
            .Handle(new GetRegistrationProviderBindingQuery(binding.TenantId, eventId, binding.Id), CancellationToken.None))!;

        await Assert.That(result.Success).IsTrue();
        await Assert.That(dto.FieldMappings.Select(mapping => mapping.PlatformFieldKey)).IsEquivalentTo(["attendee.email", "meal"]);
        await Assert.That(dto.OptionMappings).HasSingleItem();
        await Assert.That(dto.OptionMappings.Single().PlatformFieldKey).IsEqualTo("meal");
        await Assert.That(dto.OptionMappings.Single().ProviderOptionKey).IsEqualTo("opt_1");
    }

    [Test]
    public async Task ReplaceMappingsRejectsDuplicateFieldsMissingOptionsAndPublishedBinding()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        var handler = new ReplaceDraftRegistrationProviderMappingsCommandHandler(new FakeProviderRepository(binding, eventId));

        BaseCommandResponse<Guid> duplicate = await handler.Handle(new ReplaceDraftRegistrationProviderMappingsCommand(
            binding.TenantId,
            binding.Id,
            [new("email", "q1", true), new("email", "q2", false)],
            []), CancellationToken.None);
        BaseCommandResponse<Guid> missing = await handler.Handle(new ReplaceDraftRegistrationProviderMappingsCommand(
            binding.TenantId,
            binding.Id,
            [new("email", "q1", true)],
            [new("meal", "vegetarian", "opt_1")]), CancellationToken.None);
        binding.Publish(Hash(), Now.AddMinutes(1));
        BaseCommandResponse<Guid> published = await handler.Handle(new ReplaceDraftRegistrationProviderMappingsCommand(
            binding.TenantId,
            binding.Id,
            [new("name", "q_name", true)],
            []), CancellationToken.None);

        await Assert.That(duplicate.Success).IsFalse();
        await Assert.That(duplicate.FailureCode).IsEqualTo("registration_provider_duplicate_field_mapping");
        await Assert.That(missing.Success).IsFalse();
        await Assert.That(missing.FailureCode).IsEqualTo("registration_provider_option_field_not_found");
        await Assert.That(published.Success).IsFalse();
        await Assert.That(published.FailureCode).IsEqualTo("registration_provider_binding_not_draft");
    }

    [Test]
    public async Task ReplaceEventMappingsValidatesBindingEventLineageBeforeDispatchingExistingCommand()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        IMediator mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ReplaceDraftRegistrationProviderMappingsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Id = binding.Id, Success = true });
        var handler = new ReplaceEventDraftRegistrationProviderMappingsCommandHandler(new FakeProviderRepository(binding, eventId), mediator);
        var request = new ReplaceRegistrationProviderMappingsRequestDto
        {
            FieldMappings = [new() { PlatformFieldKey = "email", ProviderFieldKey = "q_email", IsRequired = true }],
            OptionMappings = []
        };

        BaseCommandResponse<Guid> denied = await handler.Handle(new(binding.TenantId, Guid.CreateVersion7(), binding.Id, request), CancellationToken.None);
        BaseCommandResponse<Guid> accepted = await handler.Handle(new(binding.TenantId, eventId, binding.Id, request), CancellationToken.None);

        await Assert.That(denied.Success).IsFalse();
        await Assert.That(denied.FailureCode).IsEqualTo("registration_provider_binding_not_found");
        await Assert.That(accepted.Success).IsTrue();
        await mediator.Received(1).Send(Arg.Is<ReplaceDraftRegistrationProviderMappingsCommand>(command =>
            command.TenantId == binding.TenantId &&
            command.BindingId == binding.Id &&
            command.Fields.Single().PlatformFieldKey == "email"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateChannel_ReusesSoftDeletedOrdinalInsteadOfFailingUniqueReadd()
    {
        RegistrationProviderBinding binding = Binding();
        Guid eventId = Guid.CreateVersion7();
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(binding.TenantId, eventId, "registration", Now);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            workflow,
            1,
            RegistrationRequirementCriticalityEnum.Required,
            false,
            RegistrationRequirementCompletionEffectEnum.BlocksRegistration,
            RegistrationAnswerSyncModeEnum.NONE,
            RegistrationRequirementSubjectTypeEnum.AllOrders,
            null,
            Now);
        RegistrationChannel deleted = RegistrationChannel.Create(requirement, 1, true, null, Now);
        requirement.AddChannel(deleted);
        deleted.Remove(Now.AddMinutes(1));
        var handler = new UpsertRegistrationChannelCommandHandler(new FakeProviderRepository(binding, eventId, requirement: requirement), new FixedTimeProvider(Now.AddMinutes(2)));

        var result = await handler.Handle(new UpsertRegistrationChannelCommand(
            binding.TenantId,
            eventId,
            workflow.Id,
            requirement.Id,
            null,
            new RegistrationChannelRequestDto { Ordinal = 1, IsNative = true }), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(deleted.Id);
        await Assert.That(deleted.IsDeleted).IsFalse();
    }

    private static RegistrationProviderBinding Binding(RegistrationProviderPresentationModeEnum presentationMode = RegistrationProviderPresentationModeEnum.Redirect) => RegistrationProviderBinding.Create(
        Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
        presentationMode,
        RegistrationProviderCollectionModeEnum.ProviderHosted,
        RegistrationProviderCompletionModeEnum.Callback,
        RegistrationProviderTrustLevelEnum.FullCanonical,
        Now);

    private static void AddLaunchCapabilities(RegistrationProviderBinding binding, string presentationCapability)
    {
        binding.AddCapability(RegistrationProviderCapability.Create(binding, "forms", "hosted", "v1", "policy", "evidence", presentationCapability));
        binding.AddCapability(RegistrationProviderCapability.Create(binding, "forms", "hosted", "v1", "policy", "evidence", RegistrationProviderCapabilityCodes.CallbackVerification));
        binding.AddCapability(RegistrationProviderCapability.Create(binding, "forms", "hosted", "v1", "policy", "evidence", RegistrationProviderCapabilityCodes.SubmissionWrite));
    }

    private static RegistrationRequirement RequirementWithChannel(RegistrationProviderBinding binding, Guid eventId, out RegistrationChannel channel)
    {
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(binding.TenantId, eventId, "registration", Now);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            workflow,
            1,
            RegistrationRequirementCriticalityEnum.Required,
            false,
            RegistrationRequirementCompletionEffectEnum.BlocksRegistration,
            RegistrationAnswerSyncModeEnum.NONE,
            RegistrationRequirementSubjectTypeEnum.AllOrders,
            null,
            Now);
        channel = RegistrationChannel.Create(requirement, 1, false, binding.Id, Now);
        requirement.AddChannel(channel);
        return requirement;
    }

    private static RegistrationEvidenceHash Hash() => RegistrationEvidenceHash.Create(Convert.ToBase64String(new byte[32]));

    private static IncomingWebhookEffectOutbox DeadLetteredEffect(RegistrationProviderBinding binding, out IncomingWebhookMessage message)
    {
        byte[] payload = Encoding.UTF8.GetBytes("{\"attemptId\":\"" + Guid.CreateVersion7().ToString("D") + "\",\"providerSubmissionId\":\"s1\",\"providerResponseRevision\":\"r1\"}");
        string hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(payload));
        string providerDecisionId = $"{binding.Id:N}:s1";
        message = IncomingWebhookMessage.CreateVerified(
            binding.TenantId,
            "registration-provider",
            providerDecisionId,
            providerDecisionId,
            ProcessProviderSubmissionEffectCommandHandler.StableEffectKind,
            payload,
            hash,
            "application/json",
            "utf-8",
            "{}",
            Now,
            Now,
            Now.AddDays(1),
            "test",
            Now.AddDays(1),
            Now.AddDays(1),
            Now.AddDays(1),
            Now.AddDays(1));
        IncomingWebhookEffectOutbox effect = IncomingWebhookEffectOutbox.CreatePending(
            binding.TenantId,
            message.Id,
            "registration-provider",
            providerDecisionId,
            ProcessProviderSubmissionEffectCommandHandler.StableEffectKind,
            hash,
            Now);
        SetMessage(effect, message);
        Guid leaseToken = Guid.CreateVersion7();
        effect.Claim("test", leaseToken, Now.AddMinutes(5), Now);
        effect.DeadLetter(leaseToken, effect.ProcessingFence, effect.ProcessingGeneration, "blocking_drift", "safe", Now.AddMinutes(1));
        return effect;
    }

    private static void SetMessage(IncomingWebhookEffectOutbox effect, IncomingWebhookMessage message) =>
        typeof(IncomingWebhookEffectOutbox).GetProperty(nameof(IncomingWebhookEffectOutbox.IncomingWebhookMessage), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(effect, message);

    private sealed record PresentationDescriptor(RegistrationProviderTuple Tuple, Uri EmbedUri) : IRegistrationProviderDescriptor, IRegistrationProviderPresentation
    {
        public RegistrationProviderCapabilitySet ProvenCapabilities => new(false, true, false, false, false, true, false, true, false, false, false, false);

        public Task<RegistrationProviderPresentationResult> GetPresentationAsync(RegistrationProviderPresentationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new RegistrationProviderPresentationResult(false, true, false, EmbedUri: EmbedUri));
    }

    private sealed class Registry(params IRegistrationProviderDescriptor[] descriptors) : IRegistrationProviderRegistry
    {
        public IRegistrationProviderDescriptor? TryResolve(RegistrationProviderTuple tuple) => descriptors.SingleOrDefault(descriptor => descriptor.Tuple == tuple);
    }

    private static RegistrationSubmission ProviderSubmission(RegistrationProviderBinding binding, Guid eventId)
    {
        RegistrationAttempt attempt = RegistrationAttempt.Create(
            Guid.CreateVersion7(), binding.TenantId, eventId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32])),
            binding.Id, RegistrationEvidenceHash.Create(Convert.ToBase64String(Enumerable.Repeat((byte)1, 32).ToArray())), Now, Now.AddHours(1));
        return RegistrationSubmission.CreateProviderEvidenceOnly(
            attempt,
            RegistrationEvidenceHash.Create(Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray())),
            Now.AddMinutes(1),
            null,
            "provider-submission-1",
            "revision-1",
            null,
            null);
    }

    private sealed class FakeProviderRepository(
        RegistrationProviderBinding binding,
        Guid eventId,
        RegistrationSubmission? submission = null,
        RegistrationProviderConnection? connection = null,
        RegistrationRequirement? requirement = null) : IRegistrationProviderRepository
    {
        public List<RegistrationSubmissionIssue> Issues { get; } = [];

        public Task<RegistrationProviderConnection?> GetConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken) => Task.FromResult(connection is not null && tenantId == connection.TenantId && connectionId == connection.Id ? connection : null);
        public Task<IReadOnlyList<RegistrationProviderConnection>> GetConnectionsAsync(Guid tenantId, CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? Task.FromCanceled<IReadOnlyList<RegistrationProviderConnection>>(cancellationToken) : Task.FromResult<IReadOnlyList<RegistrationProviderConnection>>([]);
        public Task<RegistrationProviderBinding?> GetBindingAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken) => Task.FromResult(tenantId == binding.TenantId && bindingId == binding.Id ? binding : null);
        public Task<bool> FormVersionBelongsToEventAsync(Guid tenantId, Guid requestedEventId, Guid formId, Guid formVersionId, CancellationToken cancellationToken) => Task.FromResult(tenantId == binding.TenantId && requestedEventId == eventId && formId == binding.RegistrationFormId && formVersionId == binding.RegistrationFormVersionId);
        public Task<RegistrationProviderBinding?> GetBindingForCallbackAsync(Guid bindingId, CancellationToken cancellationToken) => Task.FromResult(bindingId == binding.Id ? binding : null);
        public Task<IReadOnlyList<RegistrationProviderBinding>> GetBindingsAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RegistrationProviderBinding>>(tenantId == binding.TenantId ? [binding] : []);
        public Task<RegistrationRequirement?> GetRequirementAsync(Guid tenantId, Guid eventId, Guid workflowId, Guid requirementId, CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? Task.FromCanceled<RegistrationRequirement?>(cancellationToken) : Task.FromResult(requirement is not null && tenantId == requirement.TenantId && eventId == requirement.EventId && workflowId == requirement.RegistrationWorkflowId && requirementId == requirement.Id ? requirement : null);
        public Task<RegistrationChannel?> GetChannelAsync(Guid tenantId, Guid eventId, Guid workflowId, Guid requirementId, Guid channelId, CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? Task.FromCanceled<RegistrationChannel?>(cancellationToken) : Task.FromResult(requirement?.Channels.SingleOrDefault(channel => channel.TenantId == tenantId && channel.EventId == eventId && channel.RegistrationWorkflowId == workflowId && channel.RegistrationRequirementId == requirementId && channel.Id == channelId));
        public Task<bool> HasSubmissionForBindingAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<IReadOnlyList<RegistrationProviderBinding>> GetBindingsForEventAsync(Guid tenantId, Guid requestedEventId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RegistrationProviderBinding>>(tenantId == binding.TenantId && requestedEventId == eventId ? [binding] : []);
        public Task<DateTime?> GetLastCallbackAtAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken) => Task.FromResult<DateTime?>(null);
        public Task<int> CountParkedItemsAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<DateTime?> GetOldestPendingItemAtAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken) => Task.FromResult<DateTime?>(null);
        public Task<IReadOnlyList<RegistrationProviderParkedItem>> GetParkedItemsForEventAsync(Guid tenantId, Guid requestedEventId, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RegistrationProviderParkedItem>>([]);
        public Task<RegistrationSubmission?> GetParkedSubmissionAsync(Guid tenantId, Guid requestedEventId, Guid submissionId, CancellationToken cancellationToken) => Task.FromResult(submission is not null && tenantId == submission.TenantId && requestedEventId == submission.EventId && submissionId == submission.Id ? submission : null);
        public Task AddSubmissionIssueAsync(RegistrationSubmissionIssue issue, CancellationToken cancellationToken) { Issues.Add(issue); return Task.CompletedTask; }
        public Task AddConnectionAsync(RegistrationProviderConnection connection, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddBindingAsync(RegistrationProviderBinding registrationBinding, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddChannelAsync(RegistrationChannel channel, CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
        public Task AddSchemaRevisionAsync(RegistrationProviderSchemaRevision revision, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMessageRepository : IIncomingWebhookMessageRepository
    {
        public List<IncomingWebhookMessage> Messages { get; } = [];
        public Task<bool> TryCreateAsync(IncomingWebhookMessage message, CancellationToken cancellationToken)
        {
            if (Messages.Any(existing => existing.TenantId == message.TenantId && existing.Provider == message.Provider && existing.ProviderMessageId == message.ProviderMessageId))
            {
                return Task.FromResult(false);
            }

            Messages.Add(message);
            return Task.FromResult(true);
        }

        public Task<IncomingWebhookMessage?> GetByProviderMessageIdForUpdateAsync(Guid tenantId, string provider, string providerMessageId, CancellationToken cancellationToken) =>
            Task.FromResult(Messages.SingleOrDefault(message => message.TenantId == tenantId && message.Provider == provider && message.ProviderMessageId == providerMessageId));
        public Task<IncomingWebhookMessage?> GetByTenantAndIdForUpdateAsync(Guid tenantId, Guid incomingWebhookMessageId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<IncomingWebhookClaim>> ClaimDueAsync(IncomingWebhookClaimRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IncomingWebhookMessage?> GetActiveClaimAsync(Guid tenantId, Guid incomingWebhookMessageId, Guid leaseToken, long processingFence, int processingGeneration, DateTime observedAt, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> RefreshActiveClaimAsync(IncomingWebhookMessage message, IncomingWebhookClaim claim, DateTime observedAt, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> TryRenewClaimAsync(Guid tenantId, Guid incomingWebhookMessageId, Guid leaseToken, long processingFence, int processingGeneration, DateTime observedAt, DateTime leaseExpiresAt, CancellationToken cancellationToken) => throw new NotImplementedException();
        public void TrackAppendedEvidence(IncomingWebhookMessage message) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeEffectRepository(IEnumerable<IncomingWebhookEffectOutbox> initial) : IIncomingWebhookEffectOutboxRepository
    {
        public List<IncomingWebhookEffectOutbox> Effects { get; } = [.. initial];
        public Task<IncomingWebhookEffectOutbox?> GetByProviderIdentityAsync(Guid tenantId, string provider, string providerDecisionId, string effectKind, CancellationToken cancellationToken) =>
            Task.FromResult(Effects.SingleOrDefault(effect => effect.TenantId == tenantId && effect.Provider == provider && effect.ProviderDecisionId == providerDecisionId && effect.EffectKind == effectKind));
        public Task<IReadOnlyList<IncomingWebhookEffectClaim>> ClaimDueAsync(IncomingWebhookEffectClaimRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IncomingWebhookEffectOutbox?> GetActiveClaimAsync(IncomingWebhookEffectClaim claim, DateTime observedAt, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IncomingWebhookEffectOutbox?> GetByTenantAndIdForUpdateAsync(Guid tenantId, Guid effectOutboxId, CancellationToken cancellationToken) =>
            Task.FromResult(Effects.SingleOrDefault(effect => effect.TenantId == tenantId && effect.Id == effectOutboxId));
        public Task<IncomingWebhookEffectOutbox?> GetByTenantAndIdAsync(Guid tenantId, Guid effectOutboxId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<IncomingWebhookEffectOutbox>> GetStatusRowsAsync(Guid tenantId, int limit, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> TryRenewClaimAsync(IncomingWebhookEffectClaim claim, DateTime observedAt, DateTime leaseExpiresAt, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<int> CountDueAsync(DateTime observedAt, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<int> CountStaleAsync(DateTime observedAt, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddAsync(IncomingWebhookEffectOutbox pointer, CancellationToken cancellationToken) { Effects.Add(pointer); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeReceiptRepository(IncomingWebhookEffectReceipt? receipt = null) : IIncomingWebhookEffectReceiptRepository
    {
        public Task<IncomingWebhookEffectReceipt?> GetByIdentityAsync(Guid tenantId, Guid incomingWebhookMessageId, string effectKind, CancellationToken cancellationToken) =>
            Task.FromResult(receipt is not null && receipt.TenantId == tenantId && receipt.IncomingWebhookMessageId == incomingWebhookMessageId ? receipt : null);
        public Task AddAsync(IncomingWebhookEffectReceipt receipt, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }
}
