// ABOUTME: Covers registration-provider management fencing for retained effects, manual imports, and resolves.
// ABOUTME: Keeps queue redrive/ack tests in Application without Docker or API host dependencies.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationProviders;
using Explore.Application.Features.RegistrationProviders.Commands;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Application.Features.StorageObjects.Requests.Queries;
using Explore.Application.Models.Storage;
using Explore.Application.Responses;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
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
        AttachConnection(binding);
        binding.SetDraftProvisionedSurvey("survey-a", "revision-a");
        var repository = new FakeProviderRepository(binding, eventId);
        var canonical = new PublishRegistrationProviderBindingCommandHandler(repository);
        IMediator mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<PublishRegistrationProviderBindingCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => canonical.Handle(call.Arg<PublishRegistrationProviderBindingCommand>(), call.Arg<CancellationToken>()));
        IRegistrationProviderManagedPublishPreflight preflight = Substitute.For<IRegistrationProviderManagedPublishPreflight>();
        preflight.RunAsync(binding.TenantId, eventId, binding, Arg.Any<CancellationToken>())
            .Returns(RegistrationProviderManagedPublishPreflightResult.Success());
        var handler = new PublishEventRegistrationProviderBindingCommandHandler(
            repository,
            mediator,
            preflight,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new PublishEventRegistrationProviderBindingCommand(binding.TenantId, eventId, binding.Id),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(binding.PublishedMappingRevisionHash).IsNotNull();
    }

    [Test]
    public async Task PublishEventBinding_PreflightFailureDoesNotDispatchCanonicalPublish()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, eventId);
        IMediator mediator = Substitute.For<IMediator>();
        IRegistrationProviderManagedPublishPreflight preflight = Substitute.For<IRegistrationProviderManagedPublishPreflight>();
        preflight.RunAsync(binding.TenantId, eventId, binding, Arg.Any<CancellationToken>())
            .Returns(RegistrationProviderManagedPublishPreflightResult.Failure("registration_provider_survey_inactive"));
        var handler = new PublishEventRegistrationProviderBindingCommandHandler(
            repository,
            mediator,
            preflight,
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new PublishEventRegistrationProviderBindingCommand(binding.TenantId, eventId, binding.Id),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_survey_inactive");
        await mediator.DidNotReceive().Send(Arg.Any<PublishRegistrationProviderBindingCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishBinding_IncludesConnectionTupleAndProviderSurveyInRevisionHash()
    {
        RegistrationProviderBinding first = Binding();
        RegistrationProviderBinding second = Binding(formVersionId: first.RegistrationFormVersionId);
        AttachConnection(first, providerWorkspaceId: "workspace-a");
        AttachConnection(second, providerWorkspaceId: "workspace-b");
        first.SetDraftProvisionedSurvey("survey-a", "revision-a");
        second.SetDraftProvisionedSurvey("survey-b", "revision-a");
        var handler = new PublishRegistrationProviderBindingCommandHandler(new FakeProviderRepository(first, Guid.CreateVersion7()));
        var secondHandler = new PublishRegistrationProviderBindingCommandHandler(new FakeProviderRepository(second, Guid.CreateVersion7()));

        await handler.Handle(new(first.TenantId, first.Id, RegistrationProviderSchemaDriftClass.NoDrift, Now), CancellationToken.None);
        await secondHandler.Handle(new(second.TenantId, second.Id, RegistrationProviderSchemaDriftClass.NoDrift, Now), CancellationToken.None);

        await Assert.That(first.PublishedMappingRevisionHash).IsNotEqualTo(second.PublishedMappingRevisionHash);
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
    public async Task MirrorOnlyBinding_RequiresSubmissionSinkCapability()
    {
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            RegistrationProviderPresentationModeEnum.Redirect,
            RegistrationProviderCollectionModeEnum.MirrorOnly,
            RegistrationProviderCompletionModeEnum.Callback,
            RegistrationProviderTrustLevelEnum.SelectedFields,
            null,
            Now);
        RegistrationProviderCapabilitySet withoutSink = new(
            true, false, false, false, false, true, false, true, false, false, false, false);
        RegistrationProviderCapabilitySet withSink = withoutSink with { SubmissionSink = true };

        Type helpers = typeof(CreateRegistrationProviderBindingCommandHandler).Assembly.GetType(
            "Explore.Application.Features.RegistrationProviders.Commands.RegistrationProviderManagementHandlerHelpers")!;
        MethodInfo contractMatches = helpers.GetMethod(
            "BindingLaunchContractMatchesCapabilities",
            BindingFlags.Static | BindingFlags.Public)!;

        await Assert.That((bool)contractMatches.Invoke(null, [binding, withoutSink])!).IsFalse();
        await Assert.That((bool)contractMatches.Invoke(null, [binding, withSink])!).IsTrue();

        RegistrationProviderBinding providerApi = RegistrationProviderBinding.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            RegistrationProviderPresentationModeEnum.Redirect,
            RegistrationProviderCollectionModeEnum.ProviderApi,
            RegistrationProviderCompletionModeEnum.Callback,
            RegistrationProviderTrustLevelEnum.SelectedFields,
            null,
            Now);
        await Assert.That((bool)contractMatches.Invoke(null, [providerApi, withoutSink])!).IsFalse();
        await Assert.That((bool)contractMatches.Invoke(null, [providerApi, withSink])!).IsTrue();
    }

    [Test]
    public async Task ConnectionRequest_RequiresBothSecretBindings()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        var handler = new UpsertRegistrationProviderConnectionCommandHandler(
            new FakeProviderRepository(binding, eventId),
            new Registry(SecretCallbackDescriptor.Instance),
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
    public async Task ConnectionUpsert_GoogleAcceptsOAuthTokenBindingWithoutWebhookSecret()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        var repository = new FakeProviderRepository(binding, eventId);
        var handler = new UpsertRegistrationProviderConnectionCommandHandler(repository, new Registry(GoogleDescriptor.Instance), new FixedTimeProvider(Now));
        RegistrationProviderConnectionRequestDto request = GoogleConnectionRequest();
        request = new RegistrationProviderConnectionRequestDto
        {
            Name = request.Name,
            ProviderKindId = request.ProviderKindId,
            DeploymentKindId = request.DeploymentKindId,
            ProviderCode = request.ProviderCode,
            ProviderDeploymentCode = request.ProviderDeploymentCode,
            ApiVersion = request.ApiVersion,
            AdapterPolicyVersion = request.AdapterPolicyVersion,
            ConformanceEvidenceRevision = request.ConformanceEvidenceRevision,
            ManagementApiBaseUrl = request.ManagementApiBaseUrl,
            PublicBaseUrl = request.PublicBaseUrl,
            ProviderWorkspaceId = request.ProviderWorkspaceId,
            ApiTokenSecretBindingId = request.ApiTokenSecretBindingId,
            WebhookSecretBindingId = Guid.Empty,
            GrantedOAuthScopes = "openid email https://www.googleapis.com/auth/forms.body.readonly https://www.googleapis.com/auth/forms.responses.readonly",
            ProviderIdentity = "user:forms-owner@example.test",
            PubSubConfigurationReference = "projects/forms-project/topics/registration-watch"
        };

        BaseCommandResponse<Guid> accepted = await handler.Handle(new(binding.TenantId, eventId, null, request), CancellationToken.None);
        RegistrationProviderConnectionRequestDto driveRequest = GoogleConnectionRequest();
        driveRequest = new RegistrationProviderConnectionRequestDto
        {
            Name = driveRequest.Name,
            ProviderKindId = driveRequest.ProviderKindId,
            DeploymentKindId = driveRequest.DeploymentKindId,
            ProviderCode = driveRequest.ProviderCode,
            ProviderDeploymentCode = driveRequest.ProviderDeploymentCode,
            ApiVersion = driveRequest.ApiVersion,
            AdapterPolicyVersion = driveRequest.AdapterPolicyVersion,
            ConformanceEvidenceRevision = driveRequest.ConformanceEvidenceRevision,
            ManagementApiBaseUrl = driveRequest.ManagementApiBaseUrl,
            PublicBaseUrl = driveRequest.PublicBaseUrl,
            ProviderWorkspaceId = driveRequest.ProviderWorkspaceId,
            ApiTokenSecretBindingId = driveRequest.ApiTokenSecretBindingId,
            WebhookSecretBindingId = Guid.Empty,
            GrantedOAuthScopes = request.GrantedOAuthScopes + " https://www.googleapis.com/auth/drive",
            ProviderIdentity = request.ProviderIdentity,
            PubSubConfigurationReference = request.PubSubConfigurationReference
        };
        BaseCommandResponse<Guid> rejected = await handler.Handle(new(binding.TenantId, eventId, null, driveRequest), CancellationToken.None);

        RegistrationProviderConnection created = repository.Connections.Single();
        RegistrationProviderConnectionDto dto = await new GetRegistrationProviderConnectionQueryHandler(repository)
            .Handle(new(binding.TenantId, eventId, created.Id), CancellationToken.None) ?? throw new InvalidOperationException();

        await Assert.That(accepted.Success).IsTrue();
        await Assert.That(rejected.Success).IsFalse();
        await Assert.That(rejected.FailureCode).IsEqualTo("registration_provider_connection_validation_failed");
        await Assert.That(created.GrantedOAuthScopes).IsEqualTo("email https://www.googleapis.com/auth/forms.body.readonly https://www.googleapis.com/auth/forms.responses.readonly openid");
        await Assert.That(created.WebhookSecretBindingId).IsNull();
        await Assert.That(dto.ProviderIdentity).IsEqualTo("user:forms-owner@example.test");
        await Assert.That(dto.PubSubConfigurationReference).IsEqualTo("projects/forms-project/topics/registration-watch");
    }

    [Test]
    public async Task ConnectionUpsert_RejectsUnnecessaryGoogleWebhookSecretInsteadOfStoringIt()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        var repository = new FakeProviderRepository(binding, eventId);
        var handler = new UpsertRegistrationProviderConnectionCommandHandler(repository, new Registry(GoogleDescriptor.Instance), new FixedTimeProvider(Now));
        RegistrationProviderConnectionRequestDto request = GoogleConnectionRequest();

        BaseCommandResponse<Guid> result = await handler.Handle(new(binding.TenantId, eventId, null, request), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_connection_validation_failed");
        await Assert.That(repository.Connections).IsEmpty();
    }

    [Test]
    public async Task ConnectionUpsert_SecretCallbackProviderRequiresWebhookSecret()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        var repository = new FakeProviderRepository(binding, eventId);
        var handler = new UpsertRegistrationProviderConnectionCommandHandler(repository, new Registry(SecretCallbackDescriptor.Instance), new FixedTimeProvider(Now));
        RegistrationProviderConnectionRequestDto request = SecretCallbackConnectionRequest(Guid.Empty);

        BaseCommandResponse<Guid> missing = await handler.Handle(new(binding.TenantId, eventId, null, request), CancellationToken.None);
        request = SecretCallbackConnectionRequest(Guid.Parse("018e4e5c-7f00-7000-8000-000000000202"));
        BaseCommandResponse<Guid> accepted = await handler.Handle(new(binding.TenantId, eventId, null, request), CancellationToken.None);

        await Assert.That(missing.Success).IsFalse();
        await Assert.That(missing.FailureCode).IsEqualTo("registration_provider_connection_validation_failed");
        await Assert.That(accepted.Success).IsTrue();
        await Assert.That(repository.Connections.Single().WebhookSecretBindingId).IsEqualTo(request.WebhookSecretBindingId);
    }

    [Test]
    public async Task ConnectionCheckpointService_RecordsSuccessfulRefreshOnlyWithUtcTimestamp()
    {
        RegistrationProviderBinding binding = Binding();
        RegistrationProviderConnection connection = AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, Guid.CreateVersion7(), connection: connection);
        var service = new RegistrationProviderConnectionCheckpointService(repository, new FixedTimeProvider(Now));

        await service.RecordCredentialRefreshAsync(binding.TenantId, connection.Id, CancellationToken.None);

        await Assert.That(connection.LastCredentialRefreshAt).IsEqualTo(Now);
        await Assert.That(repository.SaveCount).IsEqualTo(1);
        await Assert.That(async () => await service.RecordCredentialRefreshAsync(Guid.CreateVersion7(), connection.Id, CancellationToken.None))
            .Throws<InvalidOperationException>();
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
    public async Task ManualImport_QueuesCsvRowsIdempotentlyAndRejectsUnsupportedCapability()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid storageObjectId = Guid.CreateVersion7();
        Guid attemptId = Guid.CreateVersion7();
        RegistrationProviderBinding supported = Binding();
        supported.AddCapability(RegistrationProviderCapability.Create(supported, "provider", "hosted", "v1", "policy", "evidence", RegistrationProviderCapabilityCodes.Manual));
        supported.SetDraftProvisionedSurvey("survey-1", null);
        AttachConnection(supported);
        RegistrationProviderBinding unsupported = Binding();
        var supportedMessages = new FakeMessageRepository();
        var supportedEffects = new FakeEffectRepository([]);
        byte[] csv = Encoding.UTF8.GetBytes($"responseId,attemptId,attemptToken,timestamp,name\r\n1,{attemptId:D},token-1,{Now:O},Amir\r\n");
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetStorageObjectContentRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => new StorageObjectContentResult(new MemoryStream(csv), "text/csv", csv.Length, Now, null));
        IRegistrationProviderCallbackReceiptProtector receiptProtector = Substitute.For<IRegistrationProviderCallbackReceiptProtector>();
        receiptProtector.Protect(Arg.Any<RegistrationProviderCallbackReceipt>()).Returns("receipt:v1:test");
        var supportedHandler = new QueueManualRegistrationProviderImportCommandHandler(
            new FakeProviderRepository(supported, eventId), supportedMessages, supportedEffects, receiptProtector, sender, new ImmediateUnitOfWork(), new FixedTimeProvider(Now));
        var unsupportedEffects = new FakeEffectRepository([]);
        var unsupportedHandler = new QueueManualRegistrationProviderImportCommandHandler(
            new FakeProviderRepository(unsupported, eventId), new FakeMessageRepository(), unsupportedEffects, receiptProtector, sender, new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var first = await supportedHandler.Handle(new(supported.TenantId, eventId, supported.Id, storageObjectId.ToString("D"), "operator-import-1"), CancellationToken.None);
        var second = await supportedHandler.Handle(new(supported.TenantId, eventId, supported.Id, storageObjectId.ToString("D"), "operator-import-1"), CancellationToken.None);
        var unsupportedResult = await unsupportedHandler.Handle(new(unsupported.TenantId, eventId, unsupported.Id, storageObjectId.ToString("D"), "operator-import-1"), CancellationToken.None);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(second.Success).IsTrue();
        await Assert.That(supportedMessages.Messages.Count).IsEqualTo(1);
        Dictionary<string, string> queuedHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(
            supportedMessages.Messages.Single().HeadersJson!)!;
        await Assert.That(queuedHeaders["X-Registration-Callback-Provider"]).IsEqualTo("forms");
        await Assert.That(queuedHeaders["X-Registration-Verification-Receipt"]).IsEqualTo("receipt:v1:test");
        await Assert.That(supportedEffects.Effects.Count).IsEqualTo(1);
        await Assert.That(supportedEffects.Effects.Single().Status).IsEqualTo(OutboxMessageStatus.Pending);
        await Assert.That(supportedEffects.Effects.Single().EffectKind).IsEqualTo(ProcessProviderSubmissionEffectCommandHandler.StableEffectKind);
        await Assert.That(unsupportedResult.Success).IsFalse();
        await Assert.That(unsupportedEffects.Effects).IsEmpty();
    }

    [Test]
    public async Task ManualImport_RejectsDuplicateCsvHeadersWithoutQueueingEffects()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid storageObjectId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        binding.AddCapability(RegistrationProviderCapability.Create(binding, "provider", "hosted", "v1", "policy", "evidence", RegistrationProviderCapabilityCodes.Manual));
        binding.SetDraftProvisionedSurvey("survey-1", null);
        AttachConnection(binding);
        byte[] csv = Encoding.UTF8.GetBytes($"responseId,attemptId,attemptToken,timestamp,responseId\r\n1,{Guid.CreateVersion7():D},token-1,{Now:O},duplicate\r\n");
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetStorageObjectContentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new StorageObjectContentResult(new MemoryStream(csv), "text/csv", csv.Length, Now, null));
        IRegistrationProviderCallbackReceiptProtector receiptProtector = Substitute.For<IRegistrationProviderCallbackReceiptProtector>();
        var effects = new FakeEffectRepository([]);
        var handler = new QueueManualRegistrationProviderImportCommandHandler(
            new FakeProviderRepository(binding, eventId), new FakeMessageRepository(), effects, receiptProtector, sender, new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new(binding.TenantId, eventId, binding.Id, storageObjectId.ToString("D"), "operator-import-1"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(effects.Effects).IsEmpty();
    }

    [Test]
    public async Task ManualImport_InvalidLaterRowLeavesNoQueuedArtifacts()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid storageObjectId = Guid.CreateVersion7();
        RegistrationProviderBinding binding = Binding();
        binding.AddCapability(RegistrationProviderCapability.Create(binding, "provider", "hosted", "v1", "policy", "evidence", RegistrationProviderCapabilityCodes.Manual));
        binding.SetDraftProvisionedSurvey("survey-1", null);
        AttachConnection(binding);
        byte[] csv = Encoding.UTF8.GetBytes(
            $"responseId,attemptId,attemptToken,timestamp\r\n1,{Guid.CreateVersion7():D},token-1,{Now:O}\r\n2,not-a-guid,token-2,{Now:O}\r\n");
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetStorageObjectContentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new StorageObjectContentResult(new MemoryStream(csv), "text/csv", csv.Length, Now, null));
        var messages = new FakeMessageRepository();
        var effects = new FakeEffectRepository([]);
        var handler = new QueueManualRegistrationProviderImportCommandHandler(
            new FakeProviderRepository(binding, eventId), messages, effects,
            Substitute.For<IRegistrationProviderCallbackReceiptProtector>(), sender, new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new(binding.TenantId, eventId, binding.Id, storageObjectId.ToString("D"), "operator-import-1"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(messages.Messages).IsEmpty();
        await Assert.That(effects.Effects).IsEmpty();
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
            RegistrationProviderDeploymentKindEnum.HostedSaas, "forms", "hosted", "v1", "policy", "evidence",
            "https:/" + "/forms.example.org/api", "https:/" + "/forms.example.org", "workspace", null, null, Now);
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
            "forms",
            "hosted",
            "v1",
            "policy",
            "evidence",
            "https:/" + "/forms.example.org/api",
            "https:/" + "/forms.example.org",
            "workspace",
            null,
            null,
            Now);
        var handler = new CreateRegistrationProviderBindingCommandHandler(
            new FakeProviderRepository(existing, eventId, connection: connection),
            Substitute.For<ISecretBindingRepository>(),
            new Registry(),
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
    public async Task CreateBinding_RejectsWebhookSecretWithoutBindingQualifierBeforePersistence()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding existing = Binding();
        RegistrationProviderConnection connection = AttachConnection(existing);
        Guid secretId = Guid.CreateVersion7();
        ISecretBindingRepository secrets = Substitute.For<ISecretBindingRepository>();
        SecretBinding wrongQualifier = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.RegistrationProviders.WebhookSecret,
            SecretScope.Tenant,
            existing.TenantId,
            "WEBHOOK_SECRET",
            qualifier: "other-binding");
        wrongQualifier.Id = secretId;
        secrets.GetByTenantAndIdAsync(existing.TenantId, secretId, Arg.Any<CancellationToken>()).Returns(wrongQualifier);
        var repository = new FakeProviderRepository(existing, eventId, connection: connection);
        var handler = new CreateRegistrationProviderBindingCommandHandler(repository, secrets, new Registry(), new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CreateRegistrationProviderBindingCommand(
            existing.TenantId,
            eventId,
            new RegistrationProviderBindingRequestDto
            {
                ConnectionId = connection.Id,
                FormId = existing.RegistrationFormId,
                FormVersionId = existing.RegistrationFormVersionId,
                ProviderWebhookId = "webhook-1",
                WebhookSecretBindingId = secretId,
                PresentationModeId = (int)RegistrationProviderPresentationModeEnum.Redirect,
                CollectionModeId = (int)RegistrationProviderCollectionModeEnum.ProviderHosted,
                CompletionModeId = (int)RegistrationProviderCompletionModeEnum.Callback,
                TrustLevelId = (int)RegistrationProviderTrustLevelEnum.FullCanonical
            }), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_binding_validation_failed");
        await Assert.That(repository.SaveCount).IsEqualTo(0);
    }

    [Test]
    public async Task CreateBinding_PersistsOnlyDescriptorProvenCapabilitiesAndIgnoresClientBooleans()
    {
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderBinding existing = Binding();
        RegistrationProviderConnection connection = AttachConnection(existing);
        var repository = new FakeProviderRepository(existing, eventId, connection: connection);
        var descriptor = new PresentationDescriptor(
            new RegistrationProviderTuple(connection.ProviderCode, connection.ProviderDeploymentCode, connection.ApiVersion, connection.AdapterPolicyVersion, connection.ConformanceEvidenceRevision),
            new Uri("https://forms.example.test/form"));
        var handler = new CreateRegistrationProviderBindingCommandHandler(
            repository,
            Substitute.For<ISecretBindingRepository>(),
            new Registry(descriptor),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(new CreateRegistrationProviderBindingCommand(
            existing.TenantId,
            eventId,
            new RegistrationProviderBindingRequestDto
            {
                ConnectionId = connection.Id,
                FormId = existing.RegistrationFormId,
                FormVersionId = existing.RegistrationFormVersionId,
                PresentationModeId = (int)RegistrationProviderPresentationModeEnum.Redirect,
                CollectionModeId = (int)RegistrationProviderCollectionModeEnum.ProviderHosted,
                CompletionModeId = (int)RegistrationProviderCompletionModeEnum.Callback,
                TrustLevelId = (int)RegistrationProviderTrustLevelEnum.FullCanonical
            }), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(repository.AddedBinding!.Capabilities.Select(capability => capability.CapabilityCode)).IsEquivalentTo([
            RegistrationProviderCapabilityCodes.Embed,
            RegistrationProviderCapabilityCodes.SubmissionWrite,
            RegistrationProviderCapabilityCodes.CallbackVerification
        ]);
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

    [Test]
    public async Task ExternalImport_CreatesPublishedFrozenExternalVersionWithRevisionSnapshot()
    {
        RegistrationProviderBinding binding = Binding();
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, eventId, connection: connection);
        var handler = ExternalImportHandler(repository, new SchemaDescriptor(connection, Snapshot([Field("email", "Email", nameof(RegistrationFieldTypeEnum.Email), true)])));

        BaseCommandResponse<Guid> result = await handler.Handle(new ImportExternalRegistrationProviderFormVersionCommand(
            binding.TenantId,
            eventId,
            connection.Id,
            new ImportExternalRegistrationProviderFormVersionRequestDto
            {
                Key = "external-registration",
                Name = "External registration",
                ProviderSurveyId = "survey-1",
                LanguageTag = "en"
            }), CancellationToken.None);

        RegistrationFormVersion version = repository.Forms.Single().Versions.Single();
        await Assert.That(result.Success).IsTrue();
        await Assert.That(version.StatusId).IsEqualTo((int)RegistrationFormStatusEnum.Published);
        await Assert.That(version.SourceKindId).IsEqualTo((int)RegistrationFormVersionSourceKindEnum.ExternalImported);
        await Assert.That(version.ExternalRegistrationProviderConnectionId).IsEqualTo(connection.Id);
        await Assert.That(version.ExternalImportMappingRevisionHash).HasLength().EqualTo(44);
        await Assert.That(repository.Revisions.Single().ProviderSnapshotSha256Hash).HasLength().EqualTo(64);
    }

    [Test]
    public async Task ExternalReimport_IdenticalSchemaReturnsExistingPublishedVersionWithoutInsertOrSave()
    {
        RegistrationProviderBinding binding = Binding();
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, eventId, connection: connection);
        var descriptor = new SchemaDescriptor(connection, Snapshot([Field("email", "Email", nameof(RegistrationFieldTypeEnum.Email), true)]));
        var request = new ImportExternalRegistrationProviderFormVersionRequestDto { Key = "external-registration", Name = "External registration", ProviderSurveyId = "survey-1", LanguageTag = "en" };
        BaseCommandResponse<Guid> first = await ExternalImportHandler(repository, descriptor).Handle(new(binding.TenantId, eventId, connection.Id, request), CancellationToken.None);
        Guid formId = repository.Forms.Single().Id;
        int saves = repository.SaveCount;

        BaseCommandResponse<Guid> replay = await ExternalImportHandler(repository, descriptor).Handle(new(binding.TenantId, eventId, connection.Id, ReimportRequest(formId)), CancellationToken.None);

        await Assert.That(replay.Success).IsTrue();
        await Assert.That(replay.Id).IsEqualTo(first.Id);
        await Assert.That(repository.Forms.Single().Versions.Count).IsEqualTo(1);
        await Assert.That(repository.Revisions.Count).IsEqualTo(1);
        await Assert.That(repository.SaveCount).IsEqualTo(saves);
    }

    [Test]
    public async Task ExternalImport_InitialReplayWithoutFormIdReturnsExistingVersionWithoutWrite()
    {
        RegistrationProviderBinding binding = Binding();
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, eventId, connection: connection);
        var descriptor = new SchemaDescriptor(connection, Snapshot([Field("email", "Email", nameof(RegistrationFieldTypeEnum.Email), true)]));
        var request = new ImportExternalRegistrationProviderFormVersionRequestDto { Key = "external-registration", Name = "External registration", ProviderSurveyId = "survey-1", LanguageTag = "en" };
        BaseCommandResponse<Guid> first = await ExternalImportHandler(repository, descriptor).Handle(new(binding.TenantId, eventId, connection.Id, request), CancellationToken.None);
        int saves = repository.SaveCount;

        BaseCommandResponse<Guid> replay = await ExternalImportHandler(repository, descriptor).Handle(new(binding.TenantId, eventId, connection.Id, request), CancellationToken.None);

        await Assert.That(replay.Success).IsTrue();
        await Assert.That(replay.Id).IsEqualTo(first.Id);
        await Assert.That(repository.Forms.Count).IsEqualTo(1);
        await Assert.That(repository.Forms.Single().Versions.Count).IsEqualTo(1);
        await Assert.That(repository.Revisions.Count).IsEqualTo(1);
        await Assert.That(repository.SaveCount).IsEqualTo(saves);
    }

    [Test]
    public async Task ExternalImport_InitialChangedSchemaWithoutFormIdReusesFormAndCreatesNextVersion()
    {
        RegistrationProviderBinding binding = Binding();
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, eventId, connection: connection);
        var request = new ImportExternalRegistrationProviderFormVersionRequestDto { Key = "external-registration", Name = "External registration", ProviderSurveyId = "survey-1", LanguageTag = "en" };
        await ExternalImportHandler(repository, new SchemaDescriptor(connection, Snapshot([Field("email", "Email", nameof(RegistrationFieldTypeEnum.Email), true)])))
            .Handle(new(binding.TenantId, eventId, connection.Id, request), CancellationToken.None);

        BaseCommandResponse<Guid> changed = await ExternalImportHandler(repository, new SchemaDescriptor(connection, Snapshot([
            Field("email", "Email", nameof(RegistrationFieldTypeEnum.Email), true),
            Field("phone", "Phone", nameof(RegistrationFieldTypeEnum.Phone), false)])))
            .Handle(new(binding.TenantId, eventId, connection.Id, request), CancellationToken.None);

        await Assert.That(changed.Success).IsTrue();
        await Assert.That(repository.Forms.Count).IsEqualTo(1);
        await Assert.That(repository.Forms.Single().Versions.Count).IsEqualTo(2);
        await Assert.That(repository.Revisions.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ExternalImport_TwoSurveysWithIdenticalSchemaCreateDistinctRevisions()
    {
        RegistrationProviderBinding binding = Binding();
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, eventId, connection: connection);
        var descriptor = new SchemaDescriptor(connection, Snapshot([Field("email", "Email", nameof(RegistrationFieldTypeEnum.Email), true)]));
        await ExternalImportHandler(repository, descriptor).Handle(new(binding.TenantId, eventId, connection.Id,
            new ImportExternalRegistrationProviderFormVersionRequestDto { Key = "survey-one", Name = "Survey one", ProviderSurveyId = "survey-1", LanguageTag = "en" }), CancellationToken.None);

        BaseCommandResponse<Guid> result = await ExternalImportHandler(repository, descriptor).Handle(new(binding.TenantId, eventId, connection.Id,
            new ImportExternalRegistrationProviderFormVersionRequestDto { Key = "survey-two", Name = "Survey two", ProviderSurveyId = "survey-2", LanguageTag = "en" }), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(repository.Revisions.Count).IsEqualTo(2);
        await Assert.That(repository.Revisions.Select(revision => revision.ProviderSurveyId).Order().SequenceEqual(["survey-1", "survey-2"])).IsTrue();
    }

    [Test]
    public async Task ExternalReimport_LabelOnlyDriftCreatesNextPublishedVersion()
    {
        RegistrationProviderBinding binding = Binding();
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, eventId, connection: connection);
        var first = ExternalImportHandler(repository, new SchemaDescriptor(connection, Snapshot([Field("email", "Email", nameof(RegistrationFieldTypeEnum.Email), true)])));
        var request = new ImportExternalRegistrationProviderFormVersionRequestDto { Key = "external-registration", Name = "External registration", ProviderSurveyId = "survey-1", LanguageTag = "en" };
        await first.Handle(new(binding.TenantId, eventId, connection.Id, request), CancellationToken.None);
        Guid formId = repository.Forms.Single().Id;
        var second = ExternalImportHandler(repository, new SchemaDescriptor(connection, Snapshot([Field("email", "Email address", nameof(RegistrationFieldTypeEnum.Email), true)])));

        BaseCommandResponse<Guid> result = await second.Handle(new(binding.TenantId, eventId, connection.Id, ReimportRequest(formId)), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(repository.Forms.Single().Versions.Count).IsEqualTo(2);
        await Assert.That(repository.Revisions.Last().DriftClassId).IsEqualTo((int)RegistrationProviderDriftClassEnum.LabelOnlyChange);
        await Assert.That(repository.Forms.Single().Versions.Select(version => version.ExternalImportMappingRevisionHash).Distinct().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task ExternalReimport_BlockingDriftRecordsRevisionWithoutVersion()
    {
        RegistrationProviderBinding binding = Binding();
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, eventId, connection: connection);
        var request = new ImportExternalRegistrationProviderFormVersionRequestDto { Key = "external-registration", Name = "External registration", ProviderSurveyId = "survey-1", LanguageTag = "en" };
        await ExternalImportHandler(repository, new SchemaDescriptor(connection, Snapshot([Field("email", "Email", nameof(RegistrationFieldTypeEnum.Email), true)])))
            .Handle(new(binding.TenantId, eventId, connection.Id, request), CancellationToken.None);
        Guid formId = repository.Forms.Single().Id;
        var blocking = ExternalImportHandler(repository, new SchemaDescriptor(connection, Snapshot([])));

        BaseCommandResponse<Guid> result = await blocking.Handle(new(binding.TenantId, eventId, connection.Id, ReimportRequest(formId)), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_provider_schema_drift_blocked");
        await Assert.That(repository.Forms.Single().Versions.Count).IsEqualTo(1);
        await Assert.That(repository.Revisions.Last().DriftClassId).IsEqualTo((int)RegistrationProviderDriftClassEnum.RequiredFieldRemoved);
    }

    [Test]
    public async Task ExternalReimport_RepeatedBlockingDriftReturnsExistingFailureWithoutInsertOrSave()
    {
        RegistrationProviderBinding binding = Binding();
        Guid eventId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = AttachConnection(binding);
        var repository = new FakeProviderRepository(binding, eventId, connection: connection);
        var request = new ImportExternalRegistrationProviderFormVersionRequestDto { Key = "external-registration", Name = "External registration", ProviderSurveyId = "survey-1", LanguageTag = "en" };
        await ExternalImportHandler(repository, new SchemaDescriptor(connection, Snapshot([Field("email", "Email", nameof(RegistrationFieldTypeEnum.Email), true)])))
            .Handle(new(binding.TenantId, eventId, connection.Id, request), CancellationToken.None);
        Guid formId = repository.Forms.Single().Id;
        var blocking = ExternalImportHandler(repository, new SchemaDescriptor(connection, Snapshot([])));
        BaseCommandResponse<Guid> firstBlock = await blocking.Handle(new(binding.TenantId, eventId, connection.Id, ReimportRequest(formId)), CancellationToken.None);
        int revisions = repository.Revisions.Count;
        int saves = repository.SaveCount;

        BaseCommandResponse<Guid> replay = await blocking.Handle(new(binding.TenantId, eventId, connection.Id, ReimportRequest(formId)), CancellationToken.None);

        await Assert.That(replay.Success).IsFalse();
        await Assert.That(replay.Id).IsEqualTo(firstBlock.Id);
        await Assert.That(replay.FailureCode).IsEqualTo("registration_provider_schema_drift_blocked");
        await Assert.That(repository.Revisions.Count).IsEqualTo(revisions);
        await Assert.That(repository.SaveCount).IsEqualTo(saves);
    }

    private static ImportExternalRegistrationProviderFormVersionCommandHandler ExternalImportHandler(FakeProviderRepository repository, IRegistrationProviderDescriptor descriptor) =>
        new(repository, new Registry(descriptor), new SchemaDriftClassifier(), new FormSchemaArtifactPublicationService(new FormSchemaArtifactGenerator()), new FixedTimeProvider(Now));

    private static RegistrationProviderSchemaSnapshot Snapshot(IReadOnlyList<RegistrationProviderSchemaFieldSnapshot> fields) => new(fields);

    private static RegistrationProviderSchemaFieldSnapshot Field(string key, string label, string type, bool required) => new(key, label, type, required, []);

    private static ImportExternalRegistrationProviderFormVersionRequestDto ReimportRequest(Guid formId) => new()
    {
        FormId = formId,
        ProviderSurveyId = "survey-1",
        LanguageTag = "en"
    };

    private static RegistrationProviderConnectionRequestDto GoogleConnectionRequest() => new()
    {
        Name = "Google Forms",
        ProviderKindId = (int)RegistrationProviderKindEnum.ExternalForm,
        DeploymentKindId = (int)RegistrationProviderDeploymentKindEnum.HostedSaas,
        ProviderCode = "GOOGLE_FORMS",
        ProviderDeploymentCode = "GOOGLE_WORKSPACE",
        ApiVersion = "v1",
        AdapterPolicyVersion = "ISLAMU_EVENT_GOOGLE_FORMS_PUBSUB_V1",
        ConformanceEvidenceRevision = "2026-08-11",
        ManagementApiBaseUrl = "https://forms.googleapis.com/v1",
        PublicBaseUrl = "https://docs.google.com",
        ProviderWorkspaceId = "google-workspace",
        ApiTokenSecretBindingId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000101"),
        WebhookSecretBindingId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000102")
    };

    private static RegistrationProviderConnectionRequestDto SecretCallbackConnectionRequest(Guid webhookSecretBindingId) => new()
    {
        Name = "Formbricks",
        ProviderKindId = (int)RegistrationProviderKindEnum.ExternalForm,
        DeploymentKindId = (int)RegistrationProviderDeploymentKindEnum.HostedSaas,
        ProviderCode = "FORMBRICKS",
        ProviderDeploymentCode = "CLOUD",
        ApiVersion = "v1",
        AdapterPolicyVersion = "ISLAMU_EVENT_FORMBRICKS_V1",
        ConformanceEvidenceRevision = "2026-08-10",
        ManagementApiBaseUrl = "https://api.formbricks.test/api/v1",
        PublicBaseUrl = "https://forms.formbricks.test",
        ProviderWorkspaceId = "workspace",
        ApiTokenSecretBindingId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000101"),
        WebhookSecretBindingId = webhookSecretBindingId
    };

    private static RegistrationProviderBinding Binding(RegistrationProviderPresentationModeEnum presentationMode = RegistrationProviderPresentationModeEnum.Redirect, Guid? formVersionId = null) => RegistrationProviderBinding.Create(
        Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), formVersionId ?? Guid.CreateVersion7(),
        presentationMode,
        RegistrationProviderCollectionModeEnum.ProviderHosted,
        RegistrationProviderCompletionModeEnum.Callback,
        RegistrationProviderTrustLevelEnum.FullCanonical,
        null,
        Now);

    private static RegistrationProviderConnection AttachConnection(RegistrationProviderBinding binding, string providerWorkspaceId = "workspace")
    {
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            binding.TenantId, "forms", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.HostedSaas, "forms", "hosted", "v1", "policy", "evidence",
            "https:/" + "/forms.example.org/api", "https:/" + "/forms.example.org", providerWorkspaceId, null, null, Now);
        typeof(RegistrationProviderBinding).GetProperty(nameof(RegistrationProviderBinding.Connection))!.SetValue(binding, connection);
        return connection;
    }

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

    private sealed record GoogleDescriptor() : IRegistrationProviderDescriptor, IRegistrationProviderDelegatedAutomation, IRegistrationProviderCallbackVerifier
    {
        public static GoogleDescriptor Instance { get; } = new();
        public RegistrationProviderTuple Tuple { get; } = new("GOOGLE_FORMS", "GOOGLE_WORKSPACE", "v1", "ISLAMU_EVENT_GOOGLE_FORMS_PUBSUB_V1", "2026-08-11");
        public RegistrationProviderCapabilitySet ProvenCapabilities { get; } = new(true, true, true, true, true, false, true, true, true, true, false, false);
        public string ConnectorContractVersion => "GOOGLE_FORMS_ENTRY_CORRELATION_V1";
        public string RequiredCorrelationPlatformFieldKey => "system.registration_attempt_token";
        public Task<RegistrationProviderCallbackVerificationResult> VerifyCallbackAsync(RegistrationProviderCallbackVerificationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new RegistrationProviderCallbackVerificationResult(true));
    }

    private sealed record SecretCallbackDescriptor() : IRegistrationProviderDescriptor, IRegistrationProviderCallbackVerifier
    {
        public static SecretCallbackDescriptor Instance { get; } = new();
        public RegistrationProviderTuple Tuple { get; } = new("FORMBRICKS", "CLOUD", "v1", "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-10");
        public RegistrationProviderCapabilitySet ProvenCapabilities { get; } = new(true, true, true, true, true, true, true, true, true, true, true, true);
        public Task<RegistrationProviderCallbackVerificationResult> VerifyCallbackAsync(RegistrationProviderCallbackVerificationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new RegistrationProviderCallbackVerificationResult(true));
    }

    private sealed record SchemaDescriptor(RegistrationProviderTuple Tuple, RegistrationProviderSchemaSnapshot Snapshot) : IRegistrationProviderDescriptor, IRegistrationProviderSchemaReader
    {
        public SchemaDescriptor(RegistrationProviderConnection connection, RegistrationProviderSchemaSnapshot snapshot)
            : this(new RegistrationProviderTuple(connection.ProviderCode, connection.ProviderDeploymentCode, connection.ApiVersion, connection.AdapterPolicyVersion, connection.ConformanceEvidenceRevision), snapshot)
        {
        }

        public RegistrationProviderCapabilitySet ProvenCapabilities => new(false, false, false, true, false, false, false, false, false, false, false, false);

        public Task<RegistrationProviderSchemaReadResult> ReadSchemaAsync(RegistrationProviderSchemaReadRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new RegistrationProviderSchemaReadResult(Snapshot, true, "revision-" + Snapshot.Fields.Count.ToString()));
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
        public List<RegistrationProviderConnection> Connections { get; } = connection is null ? [] : [connection];
        public List<RegistrationForm> Forms { get; } = [];
      public List<RegistrationProviderSchemaRevision> Revisions { get; } = [];
      public RegistrationProviderBinding? AddedBinding { get; private set; }
        public int SaveCount { get; private set; }

        public Task<RegistrationProviderConnection?> GetConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken) => Task.FromResult(Connections.SingleOrDefault(connection => tenantId == connection.TenantId && connectionId == connection.Id));
        public Task<IReadOnlyList<RegistrationProviderConnection>> GetConnectionsAsync(Guid tenantId, CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? Task.FromCanceled<IReadOnlyList<RegistrationProviderConnection>>(cancellationToken) : Task.FromResult<IReadOnlyList<RegistrationProviderConnection>>([.. Connections.Where(connection => connection.TenantId == tenantId)]);
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
        public Task<RegistrationForm?> GetFormForExternalImportAsync(Guid tenantId, Guid requestedEventId, Guid formId, CancellationToken cancellationToken) =>
            Task.FromResult(Forms.SingleOrDefault(form => form.TenantId == tenantId && form.EventId == requestedEventId && form.Id == formId));
        public Task<RegistrationForm?> GetExternalImportFormAsync(Guid tenantId, Guid requestedEventId, Guid connectionId, string providerSurveyId, CancellationToken cancellationToken) =>
            Task.FromResult(Forms.SingleOrDefault(form => form.TenantId == tenantId && form.EventId == requestedEventId && form.Versions.Any(version =>
                version.SourceKindId == (int)RegistrationFormVersionSourceKindEnum.ExternalImported &&
                version.ExternalRegistrationProviderConnectionId == connectionId &&
                (version.ExternalProviderSurveyId == providerSurveyId || Revisions.Any(revision => revision.Id == version.ExternalRegistrationProviderSchemaRevisionId && revision.ProviderSurveyId == providerSurveyId)))));
        public Task<RegistrationProviderSchemaRevision?> GetLatestExternalImportSchemaRevisionAsync(Guid tenantId, Guid requestedEventId, Guid formId, Guid connectionId, string providerSurveyId, CancellationToken cancellationToken)
        {
            Guid? revisionId = Forms.Single(form => form.Id == formId).Versions
                .Where(version => version.TenantId == tenantId && version.EventId == requestedEventId &&
                    version.SourceKindId == (int)RegistrationFormVersionSourceKindEnum.ExternalImported &&
                    version.ExternalRegistrationProviderConnectionId == connectionId &&
                    version.ExternalProviderSurveyId == providerSurveyId)
                .OrderByDescending(version => version.Version)
                .Select(version => version.ExternalRegistrationProviderSchemaRevisionId)
                .FirstOrDefault();
            return Task.FromResult(revisionId is null ? null : Revisions.Single(revision => revision.Id == revisionId));
        }
        public Task<RegistrationProviderSchemaRevision?> GetSchemaRevisionByHashAsync(Guid tenantId, Guid connectionId, string providerSurveyId, RegistrationEvidenceHash revisionHash, CancellationToken cancellationToken) =>
            Task.FromResult(Revisions.SingleOrDefault(revision => revision.TenantId == tenantId && revision.RegistrationProviderConnectionId == connectionId && revision.ProviderSurveyId == providerSurveyId && revision.RevisionHash == revisionHash));
        public Task<IReadOnlyList<RegistrationProviderParkedItem>> GetParkedItemsForEventAsync(Guid tenantId, Guid requestedEventId, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RegistrationProviderParkedItem>>([]);
        public Task<RegistrationSubmission?> GetParkedSubmissionAsync(Guid tenantId, Guid requestedEventId, Guid submissionId, CancellationToken cancellationToken) => Task.FromResult(submission is not null && tenantId == submission.TenantId && requestedEventId == submission.EventId && submissionId == submission.Id ? submission : null);
        public Task AddSubmissionIssueAsync(RegistrationSubmissionIssue issue, CancellationToken cancellationToken) { Issues.Add(issue); return Task.CompletedTask; }
        public Task AddConnectionAsync(RegistrationProviderConnection connection, CancellationToken cancellationToken) { Connections.Add(connection); return Task.CompletedTask; }
      public Task AddBindingAsync(RegistrationProviderBinding registrationBinding, CancellationToken cancellationToken) { AddedBinding = registrationBinding; return Task.CompletedTask; }
        public Task AddFormAsync(RegistrationForm form, CancellationToken cancellationToken) { Forms.Add(form); return Task.CompletedTask; }
        public Task AddChannelAsync(RegistrationChannel channel, CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
        public Task AddSchemaRevisionAsync(RegistrationProviderSchemaRevision revision, CancellationToken cancellationToken) { Revisions.Add(revision); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) { SaveCount++; return Task.CompletedTask; }
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

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            await operation(ct);

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            await operation(ct);

        public async Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            await operation(ct);
    }
}
