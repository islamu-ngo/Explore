// ABOUTME: Unit tests for guarded AI assistant send-message queuing.
// ABOUTME: Verifies governance, idempotency, quotas, stale-run release, and queued run persistence.

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
        _conversationRepository.CountTenantMessagesSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _conversationRepository.ReleaseStaleRunningConversationsForUserAsync(
                _userId,
                Arg.Any<DateTime>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(0);
        _conversationRepository.CountRunningConversationsForUserAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(0);
        _modelCatalog.ListAvailableModelsAsync(Arg.Any<CancellationToken>())
            .Returns([new AiModelDescriptor(AiProviderDefaults.FakeModelId, AiProviderDefaults.FakeModelDisplayName, SupportsToolProposals: true)]);
    }

    [Test]
    public async Task Handle_WhenUserIsUnauthenticated_FailsBeforePersistence()
    {
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unauthenticated");
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenTenantAiDisabled_FailsBeforePersistence()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: false));

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("disabled");
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenIdempotencyKeyReplays_ReturnsExistingRunWithoutLoadingConversation()
    {
        var command = CreateCommand(content: "Replay this", idempotencyKey: "idem-replay");
        var priorRunId = Guid.CreateVersion7();
        _idempotencyRepository.FindAsync("idem-replay", _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateIdempotencyRecord(command, priorRunId));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(priorRunId);
        await _conversationRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDailyQuotaExceeded_ReturnsQuotaFailureBeforePersistence()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake, dailyLimit: 1));
        _conversationRepository.CountUserMessagesSinceAsync(_userId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("quota_exceeded");
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenTenantDailyQuotaExceeded_ReturnsQuotaFailureBeforePersistence()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake, dailyTenantLimit: 10));
        _conversationRepository.CountTenantMessagesSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(10);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("quota_exceeded");
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(GovernanceSettingKeys.AiAssistant.DailyTenantMessageLimit);
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenConcurrentRunLimitExceeded_ReturnsQuotaFailureBeforePersistence()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake, concurrentRunLimit: 1));
        _conversationRepository.CountRunningConversationsForUserAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("quota_exceeded");
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(GovernanceSettingKeys.AiAssistant.ConcurrentRunLimit);
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_ReleasesStaleRunningConversationsBeforeConcurrentQuotaCheck()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake, concurrentRunLimit: 1));
        _conversationRepository.ReleaseStaleRunningConversationsForUserAsync(
                _userId,
                Arg.Any<DateTime>(),
                "stale_ai_run_released",
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(1);
        _conversationRepository.CountRunningConversationsForUserAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _conversationRepository.Received(1).ReleaseStaleRunningConversationsForUserAsync(
            _userId,
            Arg.Is<DateTime>(cutoff => cutoff < DateTime.UtcNow),
            "stale_ai_run_released",
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenProviderBlockWasRetryable_ReactivatesAndQueuesRun()
    {
        var conversation = CreateConversation();
        conversation.Block("provider_timeout", DateTime.UtcNow.AddMinutes(-1));
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(conversation.BlockedReason).IsNull();
        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Running);
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Queued);
    }

    [Test]
    public async Task Handle_WhenRequestIsValid_QueuesUserMessageRunAndIdempotencyRecord()
    {
        var actorId = Guid.CreateVersion7();
        var conversation = CreateConversation();
        IdempotencyRecord? savedIdempotency = null;
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _idempotencyRepository.SaveAsync(Arg.Do<IdempotencyRecord>(record => savedIdempotency = record), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(CreateCommand(content: "Plan the event", actorId: actorId), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("AI message queued.");
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(conversation.Status).IsEqualTo(AiConversationStatus.Running);
        await Assert.That(conversation.ActorId).IsEqualTo(actorId);
        await Assert.That(conversation.Messages.Count).IsEqualTo(1);
        await Assert.That(conversation.Messages.Single().Role).IsEqualTo(AiMessageRole.User);
        await Assert.That(conversation.Runs.Single().Status).IsEqualTo(AiRunStatus.Queued);
        await Assert.That(savedIdempotency).IsNotNull();
        await Assert.That(savedIdempotency!.ResponseBody).IsEqualTo(result.Id.ToString("D", CultureInfo.InvariantCulture));
        await Assert.That(savedIdempotency.RequestBodyHash).IsEqualTo(
            ComputeBodyHash(_conversationId, "Plan the event", AiProviderDefaults.FakeModelId, AiAssistantInteractionModes.Build, actorId));
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenModeIsAsk_IncludesModeInIdempotencyHash()
    {
        var conversation = CreateConversation();
        IdempotencyRecord? savedIdempotency = null;
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _idempotencyRepository.SaveAsync(Arg.Do<IdempotencyRecord>(record => savedIdempotency = record), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(
            CreateCommand(content: "Just answer", mode: AiAssistantInteractionModes.Ask),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(savedIdempotency).IsNotNull();
        await Assert.That(savedIdempotency!.RequestBodyHash).IsEqualTo(
            ComputeBodyHash(_conversationId, "Just answer", AiProviderDefaults.FakeModelId, AiAssistantInteractionModes.Ask));
    }

    private SendAiMessageCommandHandler CreateHandler()
        => new(
            _conversationRepository,
            _idempotencyRepository,
            _settingsResolver,
            _tenantContext,
            _currentUserService,
            _modelCatalog);

    private SendAiMessageCommand CreateCommand(
        string content = "Please help plan this event.",
        string idempotencyKey = "idem-ai-send",
        string mode = AiAssistantInteractionModes.Build,
        Guid? actorId = null)
        => new()
        {
            ConversationId = _conversationId,
            Message = new SendAiMessageRequestDto
            {
                Content = content,
                IdempotencyKey = idempotencyKey,
                Mode = mode,
                ActorId = actorId
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
        var mode = AiAssistantInteractionModes.Normalize(command.Message.Mode);
        return new IdempotencyRecord
        {
            Id = Guid.CreateVersion7(),
            Key = command.Message.IdempotencyKey.Trim(),
            TenantId = _tenantId,
            UserId = _userId.ToString("N", CultureInfo.InvariantCulture),
            RequestMethod = "AI_SEND",
            RequestTarget = $"ai/conversations/{command.ConversationId:N}/messages",
            RequestContentType = "application/json",
            RequestBodyHash = ComputeBodyHash(command.ConversationId, content, AiProviderDefaults.FakeModelId, mode, command.Message.ActorId),
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
        int dailyTenantLimit = 1000,
        int concurrentRunLimit = 1,
        int selectedReferenceLimit = 8,
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
            [GovernanceSettingKeys.AiAssistant.DailyMessageLimit] = Setting(GovernanceSettingKeys.AiAssistant.DailyMessageLimit, dailyLimit),
            [GovernanceSettingKeys.AiAssistant.DailyTenantMessageLimit] = Setting(GovernanceSettingKeys.AiAssistant.DailyTenantMessageLimit, dailyTenantLimit),
            [GovernanceSettingKeys.AiAssistant.ConcurrentRunLimit] = Setting(GovernanceSettingKeys.AiAssistant.ConcurrentRunLimit, concurrentRunLimit),
            [GovernanceSettingKeys.AiAssistant.SelectedReferenceLimit] = Setting(GovernanceSettingKeys.AiAssistant.SelectedReferenceLimit, selectedReferenceLimit),
            [GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled] = Setting(GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled, toolProposalsEnabled),
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

    private static string ComputeBodyHash(Guid conversationId, string content, string modelId, string mode, Guid? actorId = null)
    {
        var value = $"{conversationId:N}:{modelId}:{mode}:{actorId:N}:{content}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string ComputePrincipalFingerprint(Guid userId)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString("N", CultureInfo.InvariantCulture))))
            .ToLowerInvariant();
}
