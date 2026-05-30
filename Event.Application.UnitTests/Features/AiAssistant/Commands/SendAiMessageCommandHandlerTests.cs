// ABOUTME: Unit tests for guarded AI assistant send-message orchestration.
// ABOUTME: Verifies governance, idempotency, quotas, provider failures, and proposed-action persistence.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

public sealed class SendAiMessageCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _conversationId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly IIdempotencyRepository _idempotencyRepository = Substitute.For<IIdempotencyRepository>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IAiModelCatalog _modelCatalog = Substitute.For<IAiModelCatalog>();
    private readonly IAiChatProvider _chatProvider = Substitute.For<IAiChatProvider>();

    public SendAiMessageCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(_userId);
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake, toolProposalsEnabled: true));
        _idempotencyRepository.FindAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(CreateConversation());
        _conversationRepository.CountUserMessagesSinceAsync(_userId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _modelCatalog.ListAvailableModelsAsync(Arg.Any<CancellationToken>())
            .Returns([new AiModelDescriptor(AiProviderDefaults.FakeModelId, AiProviderDefaults.FakeModelDisplayName, SupportsToolProposals: true)]);
        _chatProvider.SendAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Success(new AiChatResponse(
                "Assistant response",
                [],
                new AiTokenUsage(1, 2, 3),
                "fake-request",
                "stop")));
    }

    [Test]
    public async Task Handle_WhenUserIsUnauthenticated_FailsBeforeProviderAndPersistence()
    {
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unauthenticated");
        await _chatProvider.DidNotReceive().SendAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenTenantAiDisabled_FailsBeforeProviderAndPersistence()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: false));

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("disabled");
        await _chatProvider.DidNotReceive().SendAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenIdempotencyKeyReplays_ReturnsExistingRunWithoutProviderCall()
    {
        var command = CreateCommand(content: "Replay this", idempotencyKey: "idem-replay");
        var priorRunId = Guid.CreateVersion7();
        _idempotencyRepository.FindAsync("idem-replay", _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateIdempotencyRecord(command, priorRunId));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(priorRunId);
        await _chatProvider.DidNotReceive().SendAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDailyQuotaExceeded_ReturnsQuotaFailureBeforeProviderCall()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake, dailyLimit: 1));
        _conversationRepository.CountUserMessagesSinceAsync(_userId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("quota_exceeded");
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await _chatProvider.DidNotReceive().SendAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenProviderFails_PersistsFailedRunAndBlocksConversation()
    {
        var conversation = CreateConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _chatProvider.SendAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Failure("provider_timeout", "Provider timed out.", isTransient: true));

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("provider_timeout");
        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Blocked);
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Failed);
        await _conversationRepository.Received(2).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenProviderSucceeds_PersistsMessagesRunAndIdempotencyRecord()
    {
        var conversation = CreateConversation();
        IdempotencyRecord? savedIdempotency = null;
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _idempotencyRepository.SaveAsync(Arg.Do<IdempotencyRecord>(record => savedIdempotency = record), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(CreateCommand(content: "Plan the event"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Active);
        await Assert.That(conversation.Messages.Count).IsEqualTo(2);
        await Assert.That(conversation.Messages.Select(message => message.Role).ToArray())
            .IsEquivalentTo([AiMessageRole.User, AiMessageRole.Assistant]);
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Succeeded);
        await Assert.That(savedIdempotency).IsNotNull();
        await Assert.That(savedIdempotency!.ResponseBody).IsEqualTo(result.Id.ToString("D", CultureInfo.InvariantCulture));
        await _chatProvider.Received(1).SendAsync(Arg.Is<AiChatRequest>(request =>
            request.Messages.Any(message => message.Role == AiMessageRole.User && message.Content.Contains("Plan the event")) &&
            request.Options.StreamingEnabled == false), Arg.Any<CancellationToken>());
        await _conversationRepository.Received(2).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenProviderReturnsToolCall_PersistsProposedActionOnly()
    {
        var conversation = CreateConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _chatProvider.SendAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(AiChatProviderResult.Success(new AiChatResponse(
                string.Empty,
                [new AiProposedActionCandidate(AiProposedActionKind.CreateEventDraft, "{\"title\":\"Draft\"}")],
                new AiTokenUsage(1, 2, 3))));

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(conversation.ProposedActions.Count).IsEqualTo(1);
        var action = conversation.ProposedActions.Single();
        await Assert.That(action.Kind).IsEqualTo(AiProposedActionKind.CreateEventDraft);
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Proposed);
        await Assert.That(action.ResultResourceId).IsNull();
        await _chatProvider.Received(1).SendAsync(Arg.Is<AiChatRequest>(request =>
            request.ActionSchema != null &&
            request.ActionSchema.AllowedKinds.Contains(AiProposedActionKind.CreateEventDraft)), Arg.Any<CancellationToken>());
    }

    private SendAiMessageCommandHandler CreateHandler()
        => new(
            _conversationRepository,
            _idempotencyRepository,
            _settingsResolver,
            _tenantContext,
            _currentUserService,
            _modelCatalog,
            _chatProvider);

    private SendAiMessageCommand CreateCommand(
        string content = "Please help plan this event.",
        string idempotencyKey = "idem-ai-send")
        => new()
        {
            ConversationId = _conversationId,
            Message = new SendAiMessageRequestDto
            {
                Content = content,
                IdempotencyKey = idempotencyKey
            }
        };

    private AiConversation CreateConversation()
        => new()
        {
            Id = _conversationId,
            TenantId = _tenantId,
            UserId = _userId,
            Status = AiConversationStatus.Active,
            Provider = AiProviderDefaults.ProviderFake,
            ModelId = AiProviderDefaults.FakeModelId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedBy = _userId,
            ConcurrencyStamp = Guid.CreateVersion7()
        };

    private IdempotencyRecord CreateIdempotencyRecord(SendAiMessageCommand command, Guid runId)
    {
        var content = command.Message.Content.Trim();
        return new IdempotencyRecord
        {
            Id = Guid.CreateVersion7(),
            Key = command.Message.IdempotencyKey.Trim(),
            TenantId = _tenantId,
            UserId = _userId.ToString("N", CultureInfo.InvariantCulture),
            RequestMethod = "AI_SEND",
            RequestTarget = $"ai/conversations/{command.ConversationId:N}/messages",
            RequestContentType = "application/json",
            RequestBodyHash = ComputeBodyHash(command.ConversationId, content),
            PrincipalFingerprint = ComputePrincipalFingerprint(_userId),
            StatusCode = 202,
            ResponseBody = runId.ToString("D", CultureInfo.InvariantCulture),
            ContentType = "text/plain",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(23)
        };
    }

    private static AiAssistantSettingGroup CreateSettings(
        bool enabled = false,
        string provider = AiProviderDefaults.ProviderNone,
        string endpointUrl = "",
        string apiKey = "",
        string modelId = "",
        int dailyLimit = 50,
        bool toolProposalsEnabled = false)
    {
        var settings = new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = Setting(GovernanceSettingKeys.AiAssistant.Enabled, enabled),
            [GovernanceSettingKeys.AiAssistant.Provider] = Setting(GovernanceSettingKeys.AiAssistant.Provider, provider),
            [GovernanceSettingKeys.AiAssistant.EndpointUrl] = Setting(GovernanceSettingKeys.AiAssistant.EndpointUrl, endpointUrl),
            [GovernanceSettingKeys.AiAssistant.ApiKey] = Setting(GovernanceSettingKeys.AiAssistant.ApiKey, apiKey),
            [GovernanceSettingKeys.AiAssistant.ModelId] = Setting(GovernanceSettingKeys.AiAssistant.ModelId, modelId),
            [GovernanceSettingKeys.AiAssistant.DailyMessageLimit] = Setting(GovernanceSettingKeys.AiAssistant.DailyMessageLimit, dailyLimit),
            [GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled] = Setting(GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled, toolProposalsEnabled)
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

    private static string ComputeBodyHash(Guid conversationId, string content)
    {
        var value = $"{conversationId:N}:{content}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string ComputePrincipalFingerprint(Guid userId)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString("N", CultureInfo.InvariantCulture))))
            .ToLowerInvariant();
}
