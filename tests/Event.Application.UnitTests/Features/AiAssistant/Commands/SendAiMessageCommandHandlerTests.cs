// ABOUTME: Unit tests for guarded AI assistant send-message queuing.
// ABOUTME: Verifies governance, idempotency, quotas, stale-run release, and queued run persistence.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Actors;
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
    private readonly IAiAssistantActorContextService _actorContextService = Substitute.For<IAiAssistantActorContextService>();

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
        _actorContextService.ResolveAuthorizedActorAsync(_tenantId, _userId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(call => AiAssistantActorContextResolution.Success(call.ArgAt<Guid?>(2), []));
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
    public async Task Handle_WhenRequestedModelIsNotEnabledByGovernance_FailsBeforeConversationLookup()
    {
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(
                enabled: true,
                provider: AiProviderDefaults.ProviderOpenAiCompatible,
                endpointUrl: "https://ai.example.test/v1",
                modelId: "gpt-5.4-mini",
                allowedModelIds: ["gpt-5.4-mini", "gpt-5.4"]));

        var result = await CreateHandler().Handle(CreateCommand(modelId: "unapproved-model"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("model_not_allowed");
        await Assert.That(result.Errors).Contains("Selected AI model is not allowed by tenant policy.");
        await _conversationRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
        await _idempotencyRepository.DidNotReceive().FindAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _idempotencyRepository.DidNotReceive().SaveAsync(Arg.Any<IdempotencyRecord>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRequestedModelIsEnabledByGovernance_QueuesRunWithRequestedModel()
    {
        var conversation = CreateConversation();
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(
                enabled: true,
                provider: AiProviderDefaults.ProviderOpenAiCompatible,
                endpointUrl: "https://ai.example.test/v1",
                modelId: "gpt-5.4-mini",
                allowedModelIds: ["gpt-5.4-mini", "gpt-5.4"]));
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var result = await CreateHandler().Handle(CreateCommand(modelId: "gpt-5.4"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(conversation.Runs.Single().Provider).IsEqualTo(AiProviderDefaults.ProviderOpenAiCompatible);
        await Assert.That(conversation.Runs.Single().ModelId).IsEqualTo("gpt-5.4");
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenIdempotencyKeyReplays_ReturnsExistingRunWithoutQueueingDuplicateRun()
    {
        var command = CreateCommand(content: "Replay this", idempotencyKey: "idem-replay");
        var priorRunId = Guid.CreateVersion7();
        _idempotencyRepository.FindAsync("idem-replay", _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateIdempotencyRecord(command, priorRunId));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(priorRunId);
        await _conversationRepository.Received(1).GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
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
            ComputeBodyHash(_conversationId, "Plan the event", null, null, AiProviderDefaults.FakeModelId, AiAssistantInteractionModes.Build, actorId));
        await _conversationRepository.Received(1).Update(conversation);
    }

    [Test]
    public async Task Handle_WhenActorIsNotAuthorized_FailsBeforePersistence()
    {
        var actorId = Guid.CreateVersion7();
        _actorContextService.ResolveAuthorizedActorAsync(_tenantId, _userId, actorId, Arg.Any<CancellationToken>())
            .Returns(AiAssistantActorContextResolution.Failure(
                "actor_context_not_authorized",
                "AI acting actor is not available to the authenticated user.",
                []));

        var result = await CreateHandler().Handle(CreateCommand(actorId: actorId), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("actor_context_not_authorized");
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
        await _idempotencyRepository.DidNotReceive().SaveAsync(Arg.Any<IdempotencyRecord>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenImagesAreProvided_PersistsAttachmentsAndHashesPayload()
    {
        var conversation = CreateConversation();
        IdempotencyRecord? savedIdempotency = null;
        byte[] imageBytes = ValidPngBytes();
        var imageData = Convert.ToBase64String(imageBytes);
        var images = new List<AiMessageImageInputDto>
        {
            new()
            {
                MediaType = "image/png",
                Data = imageData,
                FileName = "csharp.png",
                SizeBytes = imageBytes.LongLength
            }
        };

        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _idempotencyRepository.SaveAsync(Arg.Do<IdempotencyRecord>(record => savedIdempotency = record), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(CreateCommand(content: "Describe this picture:", images: images), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        var message = conversation.Messages.Single();
        await Assert.That(message.Content).IsEqualTo("Describe this picture:");
        await Assert.That(message.ImageAttachmentsJson).IsNotNull();
        await Assert.That(message.ImageAttachmentsJson!).Contains("\"mediaType\":\"image/png\"");
        await Assert.That(message.ImageAttachmentsJson!).Contains($"\"data\":\"{imageData}\"");
        await Assert.That(savedIdempotency).IsNotNull();
        await Assert.That(savedIdempotency!.RequestBodyHash).IsEqualTo(
            ComputeBodyHash(
                _conversationId,
                "Describe this picture:",
                CreateImageAttachmentsJson(images),
                null,
                AiProviderDefaults.FakeModelId,
                AiAssistantInteractionModes.Build));
    }

    [Test]
    public async Task Handle_WhenImageBytesDoNotMatchMime_RejectsBeforeProviderOrPersistence()
    {
        var conversation = CreateConversation();
        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        IReadOnlyList<AiMessageImageInputDto> images =
        [
            new()
            {
                MediaType = "image/png",
                Data = Convert.ToBase64String("<svg></svg>"u8.ToArray()),
                FileName = "poster.png",
                SizeBytes = 11
            }
        ];

        var result = await CreateHandler().Handle(
            CreateCommand(content: "Use this poster", images: images),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_ai_image_attachment");
        await Assert.That(conversation.Messages).IsEmpty();
        await _settingsResolver.DidNotReceive().ResolveGroupAsync<AiAssistantSettingGroup>(
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
        await _idempotencyRepository.DidNotReceive().SaveAsync(
            Arg.Any<IdempotencyRecord>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenImageMimeIsNotBrowserSafe_RejectsBeforeProviderOrPersistence()
    {
        IReadOnlyList<AiMessageImageInputDto> images =
        [
            new()
            {
                MediaType = "image/svg+xml",
                Data = Convert.ToBase64String("<svg></svg>"u8.ToArray()),
                FileName = "poster.svg",
                SizeBytes = 11
            }
        ];

        var result = await CreateHandler().Handle(
            CreateCommand(content: "Use this poster", images: images),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("validation_failed");
        await _settingsResolver.DidNotReceive().ResolveGroupAsync<AiAssistantSettingGroup>(
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenImageExtensionDoesNotMatchMime_RejectsBeforeProviderOrPersistence()
    {
        byte[] pngBytes = ValidPngBytes();
        IReadOnlyList<AiMessageImageInputDto> images =
        [
            new()
            {
                MediaType = "image/png",
                Data = Convert.ToBase64String(pngBytes),
                FileName = "poster.jpg",
                SizeBytes = pngBytes.LongLength
            }
        ];

        var result = await CreateHandler().Handle(
            CreateCommand(content: "Use this poster", images: images),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_ai_image_attachment");
        await _settingsResolver.DidNotReceive().ResolveGroupAsync<AiAssistantSettingGroup>(
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenDeclaredImageSizeDoesNotMatchBytes_RejectsBeforeProviderOrPersistence()
    {
        byte[] pngBytes = ValidPngBytes();
        IReadOnlyList<AiMessageImageInputDto> images =
        [
            new()
            {
                MediaType = "image/png",
                Data = Convert.ToBase64String(pngBytes),
                FileName = "poster.png",
                SizeBytes = pngBytes.LongLength + 1
            }
        ];

        var result = await CreateHandler().Handle(
            CreateCommand(content: "Use this poster", images: images),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_ai_image_attachment");
        await _settingsResolver.DidNotReceive().ResolveGroupAsync<AiAssistantSettingGroup>(
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().Update(Arg.Any<AiConversation>());
    }

    [Test]
    public async Task Handle_WhenReferencesAreProvided_PersistsReferencesAndHashesPayload()
    {
        var conversation = CreateConversation();
        IdempotencyRecord? savedIdempotency = null;
        var eventReferenceId = Guid.CreateVersion7();
        var actorReferenceId = Guid.CreateVersion7();
        IReadOnlyList<AiSelectedReferenceDto> references =
        [
            new("Event", eventReferenceId, "Community Iftar", "Public evening program"),
            new("Actor", actorReferenceId, "Amina Speaker", null),
            new("Event", eventReferenceId, "Community Iftar duplicate", "Duplicate should be ignored")
        ];

        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        _idempotencyRepository.SaveAsync(Arg.Do<IdempotencyRecord>(record => savedIdempotency = record), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(
            CreateCommand(content: "Draft it with these references", references: references),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(conversation.References.Count).IsEqualTo(2);
        await Assert.That(conversation.References.Select(reference => reference.Kind)).Contains(AiReferenceKind.Event);
        await Assert.That(conversation.References.Select(reference => reference.Kind)).Contains(AiReferenceKind.Actor);
        await Assert.That(conversation.References.Single(reference => reference.Kind == AiReferenceKind.Event).DisplayName).IsEqualTo("Community Iftar");
        await Assert.That(savedIdempotency).IsNotNull();
        await Assert.That(savedIdempotency!.RequestBodyHash).IsEqualTo(
            ComputeBodyHash(
                _conversationId,
                "Draft it with these references",
                null,
                CreateReferencesJson(references.Take(2).ToList()),
                AiProviderDefaults.FakeModelId,
                AiAssistantInteractionModes.Build));
    }

    [Test]
    public async Task Handle_WhenImageOnlyRequestHasNullContent_QueuesImageOnlyMessage()
    {
        var conversation = CreateConversation();
        var images = new List<AiMessageImageInputDto>
        {
            new()
            {
                MediaType = "image/png",
                Data = Convert.ToBase64String(ValidPngBytes()),
                FileName = "diagram.png",
                SizeBytes = ValidPngBytes().LongLength
            }
        };

        _conversationRepository.GetByIdForUpdateAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var command = CreateCommand(content: string.Empty, images: images);
        command.Message.Content = null!;
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        var message = conversation.Messages.Single();
        await Assert.That(message.Content).IsEqualTo(string.Empty);
        await Assert.That(message.ImageAttachmentsJson).IsNotNull();
        await Assert.That(message.ImageAttachmentsJson!).Contains("\"mediaType\":\"image/png\"");
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
            ComputeBodyHash(_conversationId, "Just answer", null, null, AiProviderDefaults.FakeModelId, AiAssistantInteractionModes.Ask));
    }

    private SendAiMessageCommandHandler CreateHandler()
        => new(
            _conversationRepository,
            _idempotencyRepository,
            _settingsResolver,
            _tenantContext,
            _currentUserService,
            _modelCatalog,
            _actorContextService);

    private SendAiMessageCommand CreateCommand(
        string content = "Please help plan this event.",
        string idempotencyKey = "idem-ai-send",
        string mode = AiAssistantInteractionModes.Build,
        Guid? actorId = null,
        string? modelId = null,
        IReadOnlyList<AiMessageImageInputDto>? images = null,
        IReadOnlyList<AiSelectedReferenceDto>? references = null)
        => new()
        {
            ConversationId = _conversationId,
            Message = new SendAiMessageRequestDto
            {
                Content = content,
                Images = images ?? [],
                IdempotencyKey = idempotencyKey,
                Mode = mode,
                ActorId = actorId,
                ModelId = modelId,
                References = references ?? []
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
        var imageAttachmentsJson = CreateImageAttachmentsJson(command.Message.Images);
        var referencesJson = CreateReferencesJson(command.Message.References);
        return new IdempotencyRecord
        {
            Id = Guid.CreateVersion7(),
            Key = command.Message.IdempotencyKey.Trim(),
            TenantId = _tenantId,
            UserId = _userId.ToString("N", CultureInfo.InvariantCulture),
            RequestMethod = "AI_SEND",
            RequestTarget = $"ai/conversations/{command.ConversationId:N}/messages",
            RequestContentType = "application/json",
            RequestBodyHash = ComputeBodyHash(command.ConversationId, content, imageAttachmentsJson, referencesJson, AiProviderDefaults.FakeModelId, mode, command.Message.ActorId),
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

    private static string ComputeBodyHash(
        Guid conversationId,
        string content,
        string? imageAttachmentsJson,
        string? selectedReferencesJson,
        string modelId,
        string mode,
        Guid? actorId = null)
    {
        var value = $"{conversationId:N}:{modelId}:{mode}:{actorId:N}:{content}:{imageAttachmentsJson}:{selectedReferencesJson}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string? CreateImageAttachmentsJson(IReadOnlyList<AiMessageImageInputDto>? images)
    {
        if (images is null || images.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(images.Select(image => new
        {
            mediaType = image.MediaType.Trim(),
            data = image.Data.Trim(),
            fileName = string.IsNullOrWhiteSpace(image.FileName) ? null : image.FileName.Trim(),
            sizeBytes = image.SizeBytes
        }), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static string? CreateReferencesJson(IReadOnlyList<AiSelectedReferenceDto>? references)
    {
        if (references is null || references.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(references.Select(reference => new
        {
            kind = reference.Kind.Trim(),
            referenceId = reference.ReferenceId,
            displayName = reference.DisplayName.Trim(),
            summary = string.IsNullOrWhiteSpace(reference.Summary) ? null : reference.Summary.Trim()
        }), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static string ComputePrincipalFingerprint(Guid userId)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString("N", CultureInfo.InvariantCulture))))
            .ToLowerInvariant();

    private static byte[] ValidPngBytes() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
}
