// ABOUTME: Unit tests for background AI assistant run processing.
// ABOUTME: Verifies provider success/failure handling and Ask vs Build tool proposal boundaries.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Handlers.Commands;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Ai;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Commands;

public sealed class ProcessAiRunCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _conversationId = Guid.CreateVersion7();
    private readonly Guid _runId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly IAiChatProvider _chatProvider = Substitute.For<IAiChatProvider>();

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
    public async Task Handle_WhenBuildModeProviderReturnsToolCall_PersistsProposedActionOnly()
    {
        var conversation = CreateQueuedConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation, conversation);
        _chatProvider.SendAsync(Arg.Any<AiChatPayload>(), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Success(new AiChatResponse(
                string.Empty,
                [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Draft\"}")],
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
                [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"ShouldNotPersist\"}")],
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
                    [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Corrected Draft\"}")],
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

    private ProcessAiRunCommandHandler CreateHandler()
        => new(_conversationRepository, _settingsResolver, _chatProvider);

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

    private static AiAssistantSettingGroup CreateSettings(
        bool enabled = false,
        string provider = AiProviderDefaults.ProviderNone,
        string endpointUrl = "",
        string apiKey = "",
        string modelId = "",
        bool toolProposalsEnabled = false,
        int timeoutSeconds = AiProviderDefaults.DefaultTimeoutSeconds)
    {
        var settings = new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = Setting(GovernanceSettingKeys.AiAssistant.Enabled, enabled),
            [GovernanceSettingKeys.AiAssistant.Provider] = Setting(GovernanceSettingKeys.AiAssistant.Provider, provider),
            [GovernanceSettingKeys.AiAssistant.EndpointUrl] = Setting(GovernanceSettingKeys.AiAssistant.EndpointUrl, endpointUrl),
            [GovernanceSettingKeys.AiAssistant.ApiKey] = Setting(GovernanceSettingKeys.AiAssistant.ApiKey, apiKey),
            [GovernanceSettingKeys.AiAssistant.ModelId] = Setting(GovernanceSettingKeys.AiAssistant.ModelId, modelId),
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
}
