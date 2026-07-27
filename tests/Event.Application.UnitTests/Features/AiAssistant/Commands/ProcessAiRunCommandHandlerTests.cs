// ABOUTME: Unit tests for background AI assistant run processing.
// ABOUTME: Verifies provider success/failure handling and Ask vs Build tool proposal boundaries.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.AiAssistant.Disclosure;
using Explore.Application.Features.AiAssistant.Handlers.Commands;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Ai;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Commands;

public sealed class ProcessAiRunCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _conversationId = Guid.CreateVersion7();
    private readonly Guid _runId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly IPrivacyErasureStateRepository _privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly IAiChatProvider _chatProvider = Substitute.For<IAiChatProvider>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IAiContextGateway _contextGateway = Substitute.For<IAiContextGateway>();
    private readonly IAiProviderTrustResolver _providerTrustResolver = Substitute.For<IAiProviderTrustResolver>();

    public ProcessAiRunCommandHandlerTests()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake, toolProposalsEnabled: true));
        _chatProvider.SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Success(new AiChatResponse(
                "Assistant response",
                [],
                new AiTokenUsage(1, 2, 3),
                "fake-request",
                "stop")));

        _providerTrustResolver.Resolve(Arg.Any<AiProviderTrustResolutionContext>())
            .Returns(AiProviderTrustTierEnum.PlatformConfiguredExternalProcessor);

        _contextGateway.Sanitize(Arg.Any<AiContextSanitizationInput>())
            .Returns(x =>
            {
                var input = x.Arg<AiContextSanitizationInput>();
                var disclosed = new List<AiContextDisclosedField>();
                foreach (var field in input.Fields)
                {
                    disclosed.Add(new AiContextDisclosedField(field.Key, field.Value, AiContextDisclosureRuleEnum.Allow));
                }
                return AiContextSanitizedEnvelope.Success(input.EntityName, disclosed, Array.Empty<string>(), Array.Empty<string>());
            });
    }

    [Test]
    public async Task Handle_WhenProviderSucceeds_PersistsAssistantMessageAndCompletesRun()
    {
        var conversation = CreateQueuedConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Active);
        await Assert.That(conversation.Messages.Count).IsEqualTo(2);
        await Assert.That(conversation.Messages.Last().Role).IsEqualTo(AiMessageRole.Assistant);
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Succeeded);
        await _conversationRepository.Received(2).Update(conversation);
        await _chatProvider.Received(1).SendAsync(Arg.Is<AiChatPayload>(payload =>
            payload.Options.StreamingEnabled == false &&
            payload.Messages.Any(message => message.Role == AiMessageRole.User && message.Content.Contains("Plan the event"))),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenConversationUserIsFenced_DoesNotSendProviderPrompt()
    {
        var conversation = CreateQueuedConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _privacyErasureStateRepository.GetBySubjectAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(CreateFencedSaga(_userId));

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Active);
        await Assert.That(conversation.Messages.Count).IsEqualTo(1);
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(0);
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Failed);
        await Assert.That(conversation.Runs.Single().FailureCode).IsEqualTo("privacy_erasure_fenced");
        await Assert.That(conversation.Runs.Single().FailureMessage).IsEqualTo("AI assistant processing is unavailable.");
        await _chatProvider.DidNotReceive().SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenConversationUserIsFencedAfterProviderResponse_DoesNotPersistProviderOutput()
    {
        var reloadedUserId = Guid.CreateVersion7();
        var startingConversation = CreateQueuedConversation();
        var fencedConversation = CreateQueuedConversation();
        fencedConversation.UserId = reloadedUserId;
        fencedConversation.Runs.Single().Start(DateTime.UtcNow.AddSeconds(-1));
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(startingConversation, fencedConversation);
        _privacyErasureStateRepository.GetBySubjectAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null);
        _privacyErasureStateRepository.GetBySubjectAsync(reloadedUserId, Arg.Any<CancellationToken>())
            .Returns(CreateFencedSaga(reloadedUserId));
        _chatProvider.SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Success(new AiChatResponse(
                "Provider text that must not persist",
                [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Provider draft\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}")],
                new AiTokenUsage(1, 2, 3))));

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await _chatProvider.Received(1).SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>());
        await Assert.That(fencedConversation.Status).IsEqualTo(AiConversationStatus.Active);
        await Assert.That(fencedConversation.Messages.Count).IsEqualTo(1);
        await Assert.That(fencedConversation.Messages.Single().Content).IsEqualTo("Plan the event");
        await Assert.That(fencedConversation.ProposedActions.Count).IsEqualTo(0);
        await Assert.That(fencedConversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Failed);
        await Assert.That(fencedConversation.Runs.Single().FailureCode).IsEqualTo("privacy_erasure_fenced");
        await Assert.That(fencedConversation.Runs.Single().FailureMessage).IsEqualTo("AI assistant processing is unavailable.");
        await _conversationRepository.Received(1).Update(startingConversation);
        await _conversationRepository.Received(1).Update(fencedConversation);
        await _privacyErasureStateRepository.Received(1).GetBySubjectAsync(_userId, Arg.Any<CancellationToken>());
        await _privacyErasureStateRepository.Received(1).GetBySubjectAsync(reloadedUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenBuildModeProviderReturnsToolCall_PersistsProposedActionOnly()
    {
        var conversation = CreateQueuedConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);
        _chatProvider.SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Success(new AiChatResponse(
                string.Empty,
                [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Draft\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}")],
                new AiTokenUsage(1, 2, 3))));

        await CreateHandler().Handle(CreateCommand(mode: AiAssistantInteractionModes.Build), CancellationToken.None);

        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
        var action = conversation.ProposedActions.Single();
        await Assert.That(action.Kind).IsEqualTo(AiProposedActionKind.CreateEventDraft);
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Proposed);
        await _chatProvider.Received(1).SendAsync(Arg.Is<AiChatPayload>(payload =>
            payload.Options.ToolProposalsEnabled &&
            payload.ActionSchema != null &&
            payload.ActionSchema.AllowedKinds.Contains(AiProposedActionKind.CreateEventDraft)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenAskModeProviderReturnsToolCall_DoesNotPersistProposedAction()
    {
        var conversation = CreateQueuedConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);
        _chatProvider.SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Success(new AiChatResponse(
                "Text-only answer",
                [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"ShouldNotPersist\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}")],
                new AiTokenUsage(1, 2, 3))));

        await CreateHandler().Handle(CreateCommand(mode: AiAssistantInteractionModes.Ask), CancellationToken.None);

        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(0);
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Succeeded);
        await _chatProvider.Received(1).SendAsync(Arg.Is<AiChatPayload>(payload =>
            !payload.Options.ToolProposalsEnabled &&
            payload.ActionSchema == null &&
            payload.SystemPrompt!.Contains("Ask mode")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenProviderFails_FailsRunAndReactivatesConversation()
    {
        var conversation = CreateQueuedConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);
        _chatProvider.SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Failure("provider_timeout", "Provider timed out.", isTransient: true));

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Active);
        await Assert.That(conversation.BlockedReason).IsNull();
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Failed);
        await Assert.That(conversation.Runs.Single().FailureCode).IsEqualTo("provider_timeout");
    }

    [Test]
    public async Task Handle_WhenQueuedRunModelIsNoLongerEnabled_FailsRunBeforeProviderCall()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(
                enabled: true,
                provider: AiProviderDefaults.ProviderOpenAiCompatible,
                endpointUrl: "https://ai.example.test/v1",
                modelId: "gpt-5.4-mini",
                allowedModelIds: ["gpt-5.4-mini"]));
        var conversation = CreateQueuedConversation(modelId: "unapproved-model");
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Active);
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Failed);
        await Assert.That(conversation.Runs.Single().FailureCode).IsEqualTo("model_not_allowed");
        await _chatProvider.DidNotReceive().SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenProviderFirstReturnsInvalidToolPayload_RetriesWithSafeCorrectionAndPersistsCorrectedProposal()
    {
        var conversation = CreateQueuedConversation();
        var capturedPayloads = new List<AiChatPayload>();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);
        _chatProvider.SendAsync(Arg.Do<AiChatPayload>(payload => capturedPayloads.Add(payload)), Arg.Any<CancellationToken>())
            .Returns(
                AiChatProviderResult.Success(new AiChatResponse(
                    string.Empty,
                    [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"description\":\"raw-invalid-secret\"}")],
                    new AiTokenUsage(1, 2, 3))),
                AiChatProviderResult.Success(new AiChatResponse(
                    string.Empty,
                    [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Corrected Draft\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}")],
                    new AiTokenUsage(1, 2, 3))));

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(capturedPayloads.Count).IsEqualTo(2);
        await Assert.That(capturedPayloads[1].Messages.Count).IsEqualTo(capturedPayloads[0].Messages.Count + 1);
        var correctionMessage = capturedPayloads[1].Messages.Last().Content;
        await Assert.That(correctionMessage).Contains("Failure code: missing_tool_argument");
        await Assert.That(correctionMessage).Contains("matches the registered schema exactly");
        await Assert.That(correctionMessage).DoesNotContain("raw-invalid-secret");
        await Assert.That(correctionMessage).DoesNotContain("description");
        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Active);
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Succeeded);
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
        await Assert.That(conversation.ProposedActions.Single().PayloadJson).Contains("Corrected Draft");
    }

    [Test]
    public async Task Handle_WhenPosterImageProviderFirstReturnsEmptyText_RetriesWithImageContextAndPersistsProposal()
    {
        var conversation = CreateQueuedConversation();
        conversation.Messages.Single().ImageAttachmentsJson = """
            [{"mediaType":"image/png","data":"aW1hZ2UtYnl0ZXM=","fileName":"poster.png","sizeBytes":11}]
            """;
        var capturedPayloads = new List<AiChatPayload>();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);
        _chatProvider.SendAsync(Arg.Do<AiChatPayload>(payload => capturedPayloads.Add(payload)), Arg.Any<CancellationToken>())
            .Returns(
                AiChatProviderResult.Failure("invalid_response", "AI provider returned an empty text response."),
                AiChatProviderResult.Success(new AiChatResponse(
                    string.Empty,
                    [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Poster Draft\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}")],
                    new AiTokenUsage(1, 2, 3))));

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(capturedPayloads.Count).IsEqualTo(2);
        await Assert.That(capturedPayloads[0].Messages.Single(message => message.Role == AiMessageRole.User).Images.Count)
            .IsEqualTo(1);
        await Assert.That(capturedPayloads[1].Messages.Any(message => message.Images.Count == 1)).IsTrue();
        await Assert.That(capturedPayloads[1].Messages.Last().Content)
            .Contains("did not include usable assistant text or a valid platform tool call");
        await Assert.That(capturedPayloads[1].Messages.Last().Content)
            .Contains("return one valid platform tool call using the provided schema");
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Succeeded);
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
        await Assert.That(conversation.ProposedActions.Single().PayloadJson).Contains("Poster Draft");
    }

    [Test]
    public async Task Handle_WhenProviderRetryStillReturnsInvalidToolPayload_FailsClosedWithSafeReasonAndReactivatesConversation()
    {
        var conversation = CreateQueuedConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);
        _chatProvider.SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>())
            .Returns(
                AiChatProviderResult.Success(new AiChatResponse(
                    string.Empty,
                    [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"description\":\"first-secret\"}")],
                    new AiTokenUsage(1, 2, 3))),
                AiChatProviderResult.Success(new AiChatResponse(
                    string.Empty,
                    [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"description\":\"second-secret\"}")],
                    new AiTokenUsage(1, 2, 3))));

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Active);
        await Assert.That(conversation.BlockedReason).IsNull();
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Failed);
        await Assert.That(conversation.Runs.Single().FailureCode).IsEqualTo("missing_tool_argument");
        await Assert.That(conversation.Runs.Single().FailureMessage).DoesNotContain("first-secret");
        await Assert.That(conversation.Runs.Single().FailureMessage).DoesNotContain("second-secret");
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_WhenRunWasCancelledBeforeProviderReturned_DoesNotOverwriteCancellation()
    {
        var startingConversation = CreateQueuedConversation();
        var cancelledConversation = CreateQueuedConversation();
        cancelledConversation.Runs.Single().Start(DateTime.UtcNow.AddSeconds(-1));
        cancelledConversation.CancelRun(cancelledConversation.Runs.Single(), DateTime.UtcNow);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(startingConversation, cancelledConversation);

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(cancelledConversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Cancelled);
        await Assert.That(cancelledConversation.Messages.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_WhenOpenAiCompatibleTimeoutIsLegacyThirtySeconds_UsesLocalProviderTimeoutFloor()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(
                enabled: true,
                provider: AiProviderDefaults.ProviderOpenAiCompatible,
                endpointUrl: "http://127.0.0.1:1337/v1",
                modelId: "Gemma-4-E2B-Uncensored-HauhauCS-Aggressive-Q8_K_P",
                timeoutSeconds: 30));
        var conversation = CreateQueuedConversation(modelId: "Gemma-4-E2B-Uncensored-HauhauCS-Aggressive-Q8_K_P");
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await _chatProvider.Received(1).SendAsync(Arg.Is<AiChatPayload>(payload =>
            payload.Options.TimeoutSeconds == AiProviderDefaults.LocalProviderTimeoutSeconds &&
            payload.ProviderConfiguration!.EndpointUrl == "http://127.0.0.1:1337/v1"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenSelectedEventReferenceExists_EnrichesPromptWithEventDetails()
    {
        var eventId = Guid.CreateVersion7();
        var conversation = CreateQueuedConversation();
        conversation.AddReference(
            AiReferenceKind.Event,
            eventId,
            "Search result title",
            "Search summary",
            _userId,
            DateTime.UtcNow.AddMinutes(-2));
        var capturedPayloads = new List<AiChatPayload>();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);
        _mediator.Send(Arg.Is<GetEventDetailsRequest>(query => query.Id == eventId), Arg.Any<CancellationToken>())
            .Returns(CreateEventDto(eventId));
        _chatProvider.SendAsync(Arg.Do<AiChatPayload>(payload => capturedPayloads.Add(payload)), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Success(new AiChatResponse(
                "Assistant response",
                [],
                new AiTokenUsage(1, 2, 3))));

        await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(capturedPayloads.Count).IsEqualTo(1);
        var referenceContext = capturedPayloads.Single().Messages.FirstOrDefault(message =>
            message.Role == AiMessageRole.System &&
            message.Content.Contains("<selected_references>", StringComparison.Ordinal));
        await Assert.That(referenceContext).IsNotNull();
        await Assert.That(referenceContext!.Content).Contains("GetById Community Iftar");
        await Assert.That(referenceContext.Content).Contains("status: Published");
        await Assert.That(referenceContext.Content).Contains("description: Detailed event context from GetById.");
        await Assert.That(referenceContext.Content).DoesNotContain("Search result title");
    }

    private ProcessAiRunCommandHandler CreateHandler()
        => new(_conversationRepository, _privacyErasureStateRepository, _settingsResolver, _chatProvider, _mediator, _contextGateway, _providerTrustResolver);

    private ProcessAiRunCommand CreateCommand(string mode = AiAssistantInteractionModes.Build)
        => new()
        {
            TenantId = _tenantId,
            ConversationId = _conversationId,
            RunId = _runId,
            Mode = mode
        };

    private AiConversation CreateQueuedConversation(string modelId = AiProviderDefaults.FakeModelId)
    {
        var conversation = new AiConversation
        {
            Id = _conversationId,
            TenantId = _tenantId,
            UserId = _userId,
            Status = AiConversationStatus.Running,
            Provider = AiProviderDefaults.ProviderFake,
            ModelId = modelId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedBy = _userId,
            ConcurrencyStamp = Guid.CreateVersion7()
        };

        conversation.Messages.Add(new AiMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = _tenantId,
            ConversationId = _conversationId,
            Sequence = 1,
            Role = AiMessageRole.User,
            Content = "Plan the event",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedBy = _userId
        });
        conversation.LastMessageSequence = 1;
        conversation.Runs.Add(new AiRun
        {
            Id = _runId,
            TenantId = _tenantId,
            ConversationId = _conversationId,
            Provider = AiProviderDefaults.ProviderFake,
            ModelId = modelId,
            Status = AiRunStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-30)
        });

        return conversation;
    }

    private static PrivacyErasureSaga CreateFencedSaga(Guid userId)
    {
        var nowUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            userId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            nowUtc,
            nowUtc);
        return PrivacyErasureSaga.Start(intent, 1, new byte[32], nowUtc.AddMinutes(5), nowUtc);
    }

    private static AiAssistantSettingGroup CreateSettings(
        bool enabled = false,
        string provider = AiProviderDefaults.ProviderNone,
        string endpointUrl = "",
        string apiKey = "",
        string modelId = "",
        bool toolProposalsEnabled = false,
        IReadOnlyList<string>? allowedModelIds = null,
        int timeoutSeconds = AiProviderDefaults.DefaultTimeoutSeconds)
    {
        var settings = new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = Setting(GovernanceSettingKeys.AiAssistant.Enabled, enabled),
            [GovernanceSettingKeys.AiAssistant.Provider] = Setting(GovernanceSettingKeys.AiAssistant.Provider, provider),
            [GovernanceSettingKeys.AiAssistant.EndpointUrl] = Setting(GovernanceSettingKeys.AiAssistant.EndpointUrl, endpointUrl),
            [GovernanceSettingKeys.AiAssistant.ApiKey] = Setting(GovernanceSettingKeys.AiAssistant.ApiKey, apiKey),
            [GovernanceSettingKeys.AiAssistant.ModelId] = Setting(GovernanceSettingKeys.AiAssistant.ModelId, modelId),
            [GovernanceSettingKeys.AiAssistant.AllowedModelIds] = Setting(
                GovernanceSettingKeys.AiAssistant.AllowedModelIds,
                allowedModelIds ?? Array.Empty<string>()),
            [GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled] = Setting(GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled, toolProposalsEnabled),
            [GovernanceSettingKeys.AiAssistant.MaxInputTokens] = Setting(GovernanceSettingKeys.AiAssistant.MaxInputTokens, AiProviderDefaults.DefaultMaxInputTokens),
            [GovernanceSettingKeys.AiAssistant.MaxOutputTokens] = Setting(GovernanceSettingKeys.AiAssistant.MaxOutputTokens, AiProviderDefaults.DefaultMaxOutputTokens),
            [GovernanceSettingKeys.AiAssistant.TimeoutSeconds] = Setting(GovernanceSettingKeys.AiAssistant.TimeoutSeconds, timeoutSeconds)
        };

        var group = new AiAssistantSettingGroup();
        group.Populate(settings);
        return group;
    }

    private static ResolvedSetting Setting(string key, object value) => new()
    {
        Key = key,
        Value = System.Text.Json.JsonSerializer.Serialize(value),
        Source = SettingSource.SystemDefault,
        IsLocked = false
    };

    private EventDto CreateEventDto(Guid eventId)
        => new()
        {
            Id = eventId,
            ConcurrencyStamp = Guid.CreateVersion7(),
            Title = "GetById Community Iftar",
            Subtitle = "Full detail title",
            Description = "Detailed event context from GetById.",
            ActorId = Guid.CreateVersion7(),
            ActorDisplayName = "ISLAMU",
            ActorTypeId = 2,
            ActorTypeFullName = "Organization",
            EventStatusId = 2,
            EventStatusFullName = "Published",
            EventStatusMasterCode = "published",
            VisibilityTypeId = 1,
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "public",
            EventFormatId = 1,
            EventFormatFullName = "In person",
            EventFormatMasterCode = "in_person",
            FirstSessionDate = new DateOnly(2026, 6, 26),
            LastSessionDate = new DateOnly(2026, 6, 26),
            Timezone = "Europe/Brussels",
            SessionCount = 1,
            TenantId = _tenantId
        };
}
