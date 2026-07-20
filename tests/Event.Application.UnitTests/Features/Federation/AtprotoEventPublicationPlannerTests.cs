// ABOUTME: Verifies the single network-free ATProto event publication planning gate and immutable outbox creation.
// ABOUTME: Proves disabled, unconsented, unlinked, invalid, and orphan update/delete paths create no PDS work.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Services.Federation;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Federation;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Federation;

public sealed class AtprotoEventPublicationPlannerTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid EventId = Guid.CreateVersion7();
    private static readonly Guid SourceVersion = Guid.CreateVersion7();
    private static readonly Guid OutboxId = Guid.CreateVersion7();
    private static readonly DateTime CreatedAt = new(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);
    private const string Did = "did:plc:publisher";
    private const string Payload = "{\"$type\":\"community.lexicon.calendar.event\",\"name\":\"Published\",\"createdAt\":\"2026-07-19T12:00:00Z\"}";
    private const string PayloadHash = "c1d4f4c2f73a18c7131e7dbfb3bdac96858ddac9c1be001b916760f90c658dd1";

    [Test]
    [Arguments(false, true, true, true, true, "capability_disabled")]
    [Arguments(true, false, true, true, true, "consent_missing")]
    [Arguments(true, true, false, true, true, "session_missing")]
    [Arguments(true, true, true, false, true, "account_not_linked")]
    [Arguments(true, true, true, true, false, "payload_invalid")]
    public async Task PlanAsync_IneligibleInitialPublication_CreatesNoOutbox(
        bool enabled,
        bool consent,
        bool hasSession,
        bool hasLink,
        bool validPayload,
        string expectedReason)
    {
        var logger = new CapturingLogger();
        PlannerFixture fixture = CreateFixture(enabled, consent, hasSession, hasLink, validPayload, logger);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanEventAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo(expectedReason);
        await Assert.That(logger.Messages).Count().IsEqualTo(1);
        await Assert.That(logger.Messages.Single()).Contains(expectedReason);
        await fixture.Outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await fixture.Outbox.DidNotReceiveWithAnyArgs().SupersedePriorAsync(default, default!, default, default, default, default);
    }

    [Test]
    public async Task PlanAsync_EligibleInitialPublication_StoresStableImmutableOutbox()
    {
        PlannerFixture fixture = CreateFixture(enabled: true, consent: true, hasSession: true, hasLink: true, validPayload: true);
        PdsSyncOutbox? saved = null;
        fixture.Outbox.AddAsync(Arg.Any<PdsSyncOutbox>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                saved = call.Arg<PdsSyncOutbox>();
                return Task.CompletedTask;
            });

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanEventAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.Id).IsEqualTo(OutboxId);
        await Assert.That(saved.TenantId).IsEqualTo(TenantId);
        await Assert.That(saved.UserId).IsEqualTo(UserId);
        await Assert.That(saved.Did).IsEqualTo(Did);
        await Assert.That(saved.Collection).IsEqualTo(AtprotoEventPublicationPlanner.EventCollection);
        await Assert.That(saved.RecordKey).IsEqualTo(OutboxId.ToString("N"));
        await Assert.That(saved.SourceVersion).IsEqualTo(SourceVersion);
        await Assert.That(saved.Payload).IsEqualTo(Payload);
        await Assert.That(saved.PayloadHash).IsEqualTo(PayloadHash);
        await Assert.That(saved.Status).IsEqualTo(PdsSyncStatus.Pending);
        await fixture.Outbox.Received(1).SupersedePriorAsync(
            TenantId,
            AtprotoEventPublicationPlanner.EventSourceType,
            EventId,
            OutboxId,
            CreatedAt,
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(PdsSyncOperation.Update)]
    [Arguments(PdsSyncOperation.Delete)]
    public async Task PlanAsync_ExistingRecordOperationWithoutOwnership_NeverSynthesizesRemoteCreate(
        PdsSyncOperation operation)
    {
        PlannerFixture fixture = CreateFixture(enabled: true, consent: true, hasSession: true, hasLink: true, validPayload: true);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanEventAsync(
            Request(operation),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo("remote_record_missing");
        await fixture.Outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Test]
    public async Task PlanAsync_UpdateRacingUnsettledCreate_EnqueuesDelayedSameIdentityUpdate()
    {
        PlannerFixture fixture = CreateFixture(true, true, true, true, true);
        PdsSyncOutbox prior = UnsettledCreate(PdsSyncStatus.Processing, CreatedAt.AddSeconds(90));
        fixture.Outbox.GetLatestUnsettledMutationAsync(
                TenantId,
                AtprotoEventPublicationPlanner.EventSourceType,
                EventId,
                AtprotoEventPublicationPlanner.EventCollection,
                Arg.Any<CancellationToken>())
            .Returns(prior);
        PdsSyncOutbox? saved = null;
        fixture.Outbox.AddAsync(Arg.Do<PdsSyncOutbox>(value => saved = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanEventAsync(
            Request(PdsSyncOperation.Update),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.Operation).IsEqualTo(PdsSyncOperation.Update);
        await Assert.That(saved.RecordKey).IsEqualTo(prior.RecordKey);
        await Assert.That(saved.Did).IsEqualTo(prior.Did);
        await Assert.That(saved.PdsHost).IsEqualTo(prior.PdsHost);
        await Assert.That(saved.ExpectedCid).IsNull();
        await Assert.That(saved.NextRetryAt).IsNotNull();
        await Assert.That(saved.NextRetryAt!.Value).IsGreaterThan(prior.LeaseExpiresAt!.Value);
    }

    [Test]
    public async Task PlanAsync_VisibilityTightensWhileCreateInFlight_EnqueuesDelayedSameIdentityDelete()
    {
        PlannerFixture fixture = CreateFixture(true, true, true, true, true);
        PdsSyncOutbox prior = UnsettledCreate(PdsSyncStatus.Processing, CreatedAt.AddSeconds(90));
        fixture.Outbox.GetLatestUnsettledMutationAsync(
                TenantId,
                AtprotoEventPublicationPlanner.EventSourceType,
                EventId,
                AtprotoEventPublicationPlanner.EventCollection,
                Arg.Any<CancellationToken>())
            .Returns(prior);
        fixture.Events.GetAtprotoPublicationGraphAsync(TenantId, EventId, Arg.Any<CancellationToken>())
            .Returns((AtprotoEventPublicationEntityGraph?)null);
        fixture.Events.GetAtprotoLifecycleStateAsync(TenantId, EventId, Arg.Any<CancellationToken>())
            .Returns(Graph().Event);
        PdsSyncOutbox? saved = null;
        fixture.Outbox.AddAsync(Arg.Do<PdsSyncOutbox>(value => saved = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanEventAsync(
            Request(PdsSyncOperation.Update),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(saved!.Operation).IsEqualTo(PdsSyncOperation.Delete);
        await Assert.That(saved.Payload).IsNull();
        await Assert.That(saved.RecordKey).IsEqualTo(prior.RecordKey);
        await Assert.That(saved.NextRetryAt).IsNotNull();
        await Assert.That(saved.NextRetryAt!.Value).IsGreaterThan(prior.LeaseExpiresAt!.Value);
        AtprotoDeliveryGateResult gate = await fixture.Planner.CheckDeliveryAsync(
            saved,
            new DateTimeOffset(CreatedAt),
            CancellationToken.None);
        await Assert.That(gate.Allowed).IsTrue();
    }

    [Test]
    public async Task PlanAsync_RestoreOnlyWithoutTombstonedOwnership_CreatesNoOutbox()
    {
        PlannerFixture fixture = CreateFixture(true, true, true, true, true);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanEventAsync(
            Request(PdsSyncOperation.Create) with { RestoreOnly = true },
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo("restore_ownership_missing");
        await fixture.Outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Test]
    public async Task PlanAsync_RestoreOnlyWithTombstone_ReusesOriginalOwnerAndStableIdentity()
    {
        PlannerFixture fixture = CreateFixture(true, true, true, true, true);
        Guid moderatorUserId = Guid.CreateVersion7();
        var tombstone = new AtprotoRecord
        {
            Id = Guid.CreateVersion7(),
            Did = Did,
            Collection = AtprotoEventPublicationPlanner.EventCollection,
            RecordKey = "stable-restored-key",
            Uri = $"at://{Did}/{AtprotoEventPublicationPlanner.EventCollection}/stable-restored-key",
            Direction = AtprotoRecordDirection.Outbound,
            Provenance = AtprotoRecordProvenance.LocalLifecycle,
            TombstonedAt = CreatedAt.AddMinutes(-1),
            UpdatedAt = CreatedAt.AddMinutes(-1)
        };
        fixture.Records.GetOwnedRecordForSourceAsync(
                TenantId,
                AtprotoEventPublicationPlanner.EventSourceType,
                EventId,
                Arg.Any<CancellationToken>())
            .Returns(new AtprotoOutboundRecordOwnership
            {
                AtprotoRecordId = tombstone.Id,
                TenantId = TenantId,
                UserId = UserId,
                SourceEntityType = AtprotoEventPublicationPlanner.EventSourceType,
                SourceEntityId = EventId,
                SourceVersion = Guid.CreateVersion7(),
                AtprotoRecord = tombstone
            });
        PdsSyncOutbox? saved = null;
        fixture.Outbox.AddAsync(Arg.Do<PdsSyncOutbox>(value => saved = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanEventAsync(
            Request(PdsSyncOperation.Create) with { UserId = moderatorUserId, RestoreOnly = true },
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.UserId).IsEqualTo(UserId);
        await Assert.That(saved.Did).IsEqualTo(Did);
        await Assert.That(saved.RecordKey).IsEqualTo(tombstone.RecordKey);
        await Assert.That(saved.AtprotoRecordId).IsEqualTo(tombstone.Id);
    }

    [Test]
    public async Task PlanAsync_UnmoderationSupersedingDelayedDelete_InheritsSafetyWindow()
    {
        PlannerFixture fixture = CreateFixture(true, true, true, true, true);
        DateTime inheritedSafetyWindow = CreatedAt.AddMinutes(3);
        PdsSyncOutbox prior = UnsettledCreate(
            PdsSyncStatus.Pending,
            leaseExpiresAt: null,
            PdsSyncOperation.Delete,
            inheritedSafetyWindow);
        fixture.Outbox.GetLatestUnsettledMutationAsync(
                TenantId,
                AtprotoEventPublicationPlanner.EventSourceType,
                EventId,
                AtprotoEventPublicationPlanner.EventCollection,
                Arg.Any<CancellationToken>())
            .Returns(prior);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanEventAsync(
            Request(PdsSyncOperation.Create) with { RestoreOnly = true },
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(result.Outbox!.Operation).IsEqualTo(PdsSyncOperation.Update);
        await Assert.That(result.Outbox.RecordKey).IsEqualTo(prior.RecordKey);
        await Assert.That(result.Outbox.NextRetryAt).IsEqualTo(inheritedSafetyWindow);
    }

    [Test]
    public async Task PlanAsync_SizeSkip_EmitsBoundedStructuredStatusWithoutSensitiveContext()
    {
        var logger = new CapturingLogger();
        PlannerFixture fixture = CreateFixture(true, true, true, true, false, logger);

        await fixture.Planner.PlanEventAsync(Request(PdsSyncOperation.Create), CancellationToken.None);

        await Assert.That(logger.Messages).Count().IsEqualTo(1);
        string warning = logger.Messages.Single();
        await Assert.That(warning).Contains(AtprotoEventPublicationPlanner.EventSourceType);
        await Assert.That(warning).Contains(nameof(PdsSyncOperation.Create));
        await Assert.That(warning).Contains("payload_invalid");
        await Assert.That(warning).DoesNotContain(Did);
        await Assert.That(warning).DoesNotContain("pds.example.test");
        await Assert.That(warning).DoesNotContain(Payload);
    }

    [Test]
    public async Task PlanAsync_MissingProjection_EmitsBoundedSourceVersionWarning()
    {
        var logger = new CapturingLogger();
        PlannerFixture fixture = CreateFixture(true, true, true, true, true, logger);
        fixture.Events.GetAtprotoPublicationGraphAsync(TenantId, EventId, Arg.Any<CancellationToken>())
            .Returns((AtprotoEventPublicationEntityGraph?)null);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanEventAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo("source_version_changed");
        await Assert.That(logger.Messages.Single()).Contains("source_version_changed");
    }

    [Test]
    [Arguments("capability", "capability_disabled")]
    [Arguments("consent", "consent_missing")]
    [Arguments("session", "session_missing")]
    [Arguments("link", "account_not_linked")]
    [Arguments("source", "source_version_changed")]
    [Arguments("payload", "privacy_or_payload_changed")]
    public async Task DeliveryGate_WhenAuthorityOrSourceChanges_ProcessorNeverCallsGateway(
        string scenario,
        string expectedReason)
    {
        PlannerFixture fixture = CreateFixture(true, true, true, true, true);
        AtprotoPublicationPlanningResult planned = await fixture.Planner.PlanEventAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);
        PdsSyncOutbox outbox = planned.Outbox!;
        switch (scenario)
        {
            case "capability":
                fixture.Settings.ResolveBatchAsync(
                        Arg.Any<IEnumerable<string>>(),
                        Arg.Any<SettingContext>(),
                        Arg.Any<CancellationToken>())
                    .Returns(call => ResolveSettings(call.Arg<IEnumerable<string>>(), false, true));
                break;
            case "consent":
                fixture.Settings.ResolveBatchAsync(
                        Arg.Any<IEnumerable<string>>(),
                        Arg.Any<SettingContext>(),
                        Arg.Any<CancellationToken>())
                    .Returns(call => ResolveSettings(call.Arg<IEnumerable<string>>(), true, false));
                break;
            case "session":
                fixture.Sessions.GetAtprotoSessionsForReadAsync(
                        TenantId,
                        UserId,
                        RepositoryBackedAtprotoSession.Provider,
                        Arg.Any<CancellationToken>())
                    .Returns([]);
                break;
            case "link":
                UserExternalLogin mismatched = LinkedLogin();
                mismatched.UserId = Guid.CreateVersion7();
                fixture.Logins.GetByProviderAndKey(RepositoryBackedAtprotoSession.Provider, Did)
                    .Returns(mismatched);
                break;
            case "source":
                Explore.Domain.Event changed = Graph().Event;
                changed.ConcurrencyStamp = Guid.CreateVersion7();
                fixture.Events.GetAtprotoLifecycleStateAsync(TenantId, EventId, Arg.Any<CancellationToken>())
                    .Returns(changed);
                break;
            case "payload":
                fixture.Payloads.BuildEventAsync(
                        Arg.Any<AtprotoEventPublicationEntityGraph>(),
                        Arg.Any<DateTimeOffset>(),
                        Arg.Any<CancellationToken>())
                    .Returns(AtprotoPublicationPayloadBuildResult.Valid(
                        new(Payload + " ", new string('7', 64))));
                break;
        }

        var repository = Substitute.For<IPdsSyncOutboxRepository>();
        var gateway = Substitute.For<IAtprotoPdsDeliveryGateway>();
        var claim = new PdsSyncClaim(
            outbox.Id,
            outbox.TenantId,
            outbox.UserId,
            Guid.CreateVersion7(),
            1);
        repository.GetActiveClaimAsync(claim, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(outbox);
        repository.TryFailAsync(
                claim,
                expectedReason,
                false,
                Arg.Any<DateTime>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var processor = new AtprotoPdsDeliveryProcessor(
            repository,
            fixture.Planner,
            gateway,
            TimeProvider.System);

        AtprotoPdsClaimResult result = await processor.ProcessAsync(
            claim,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(AtprotoPdsClaimOutcome.DeliveryFailed);
        await Assert.That(result.FailureCode).IsEqualTo(expectedReason);
        await gateway.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
    }

    [Test]
    [Category("EventLocationPrivacyExternal")]
    public async Task PlanLocationPrivacyCorrectionAsync_WithOlderRemoteState_EnqueuesCurrentProjection()
    {
        PlannerFixture fixture = CreateFixture(true, true, true, true, true);
        PdsSyncOutbox prior = UnsettledCreate(PdsSyncStatus.Pending, leaseExpiresAt: null);
        fixture.Outbox.GetLatestUnsettledMutationAsync(
                TenantId,
                AtprotoEventPublicationPlanner.EventSourceType,
                EventId,
                AtprotoEventPublicationPlanner.EventCollection,
                Arg.Any<CancellationToken>())
            .Returns(prior);
        PdsSyncOutbox? saved = null;
        fixture.Outbox.AddAsync(Arg.Do<PdsSyncOutbox>(value => saved = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanLocationPrivacyCorrectionAsync(
            new(TenantId, EventId, OutboxId, CreatedAt),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.Id).IsEqualTo(OutboxId);
        await Assert.That(saved.Operation).IsEqualTo(PdsSyncOperation.Update);
        await Assert.That(saved.SourceVersion).IsEqualTo(SourceVersion);
        await Assert.That(saved.Payload).IsEqualTo(Payload);
    }

    [Test]
    [Category("EventLocationPrivacyExternal")]
    public async Task PlanLocationPrivacyCorrectionAsync_WhenReceiptAlreadyExists_IsIdempotent()
    {
        PlannerFixture fixture = CreateFixture(true, true, true, true, true);
        fixture.Outbox.ExistsAsync(TenantId, OutboxId, Arg.Any<CancellationToken>())
            .Returns(true);

        AtprotoPublicationPlanningResult first = await fixture.Planner.PlanLocationPrivacyCorrectionAsync(
            new(TenantId, EventId, OutboxId, CreatedAt),
            CancellationToken.None);
        AtprotoPublicationPlanningResult replay = await fixture.Planner.PlanLocationPrivacyCorrectionAsync(
            new(TenantId, EventId, OutboxId, CreatedAt),
            CancellationToken.None);

        await Assert.That(first.Enqueued).IsFalse();
        await Assert.That(first.ReasonCode).IsEqualTo("correction_already_planned");
        await Assert.That(replay.ReasonCode).IsEqualTo("correction_already_planned");
        await fixture.Outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Test]
    [Category("EventLocationPrivacyExternal")]
    public async Task PlanLocationPrivacyCorrectionAsync_WhenSourceIsMissing_EnqueuesRemoteDelete()
    {
        PlannerFixture fixture = CreateFixture(true, true, true, true, true);
        PdsSyncOutbox prior = UnsettledCreate(PdsSyncStatus.Pending, leaseExpiresAt: null);
        fixture.Events.GetAtprotoLifecycleStateAsync(TenantId, EventId, Arg.Any<CancellationToken>())
            .Returns((Explore.Domain.Event?)null);
        fixture.Outbox.GetLatestUnsettledMutationAsync(
                TenantId,
                AtprotoEventPublicationPlanner.EventSourceType,
                EventId,
                AtprotoEventPublicationPlanner.EventCollection,
                Arg.Any<CancellationToken>())
            .Returns(prior);
        PdsSyncOutbox? saved = null;
        fixture.Outbox.AddAsync(Arg.Do<PdsSyncOutbox>(value => saved = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanLocationPrivacyCorrectionAsync(
            new(TenantId, EventId, OutboxId, CreatedAt),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.Operation).IsEqualTo(PdsSyncOperation.Delete);
        await Assert.That(saved.Payload).IsNull();
    }

    private static PlannerFixture CreateFixture(
        bool enabled,
        bool consent,
        bool hasSession,
        bool hasLink,
        bool validPayload,
        ILogger<AtprotoEventPublicationPlanner>? logger = null)
    {
        var settings = Substitute.For<IHierarchicalSettingsResolver>();
        settings.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ResolveSettings(
                call.Arg<IEnumerable<string>>(),
                enabled,
                consent));

        var events = Substitute.For<IEventRepository>();
        events.GetAtprotoPublicationGraphAsync(TenantId, EventId, Arg.Any<CancellationToken>())
            .Returns(Graph());
        events.GetAtprotoLifecycleStateAsync(TenantId, EventId, Arg.Any<CancellationToken>())
            .Returns(Graph().Event);
        var records = Substitute.For<IAtprotoRecordRepository>();
        var sessions = Substitute.For<IUserAuthenticationTokenRepository>();
        sessions.GetAtprotoSessionsForReadAsync(TenantId, UserId, RepositoryBackedAtprotoSession.Provider, Arg.Any<CancellationToken>())
            .Returns(hasSession ? [Session()] : []);
        var logins = Substitute.For<IUserExternalLoginRepository>();
        logins.GetByProviderAndKey(RepositoryBackedAtprotoSession.Provider, Did)
            .Returns(hasLink ? LinkedLogin() : null);
        var payloads = Substitute.For<IAtprotoPublicationPayloadBuilder>();
        payloads.BuildEventAsync(Arg.Any<AtprotoEventPublicationEntityGraph>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(validPayload
                ? AtprotoPublicationPayloadBuildResult.Valid(new(Payload, PayloadHash))
                : AtprotoPublicationPayloadBuildResult.Invalid("payload_invalid"));
        var outbox = Substitute.For<IPdsSyncOutboxRepository>();
        var planner = new AtprotoEventPublicationPlanner(
            new AtprotoEventGovernanceResolver(settings),
            events,
            Substitute.For<IEventRegistrationIntentRepository>(),
            records,
            sessions,
            logins,
            payloads,
            outbox,
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AtprotoEventPublicationPlanner>.Instance);
        return new(planner, settings, events, records, sessions, logins, payloads, outbox);
    }

    private static AtprotoEventPublicationInput Request(PdsSyncOperation operation) => new(
        TenantId,
        UserId,
        EventId,
        SourceVersion,
        operation,
        OutboxId,
        CreatedAt);

    private static UserAuthenticationToken Session() => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = TenantId,
        Tenant = null!,
        UserId = UserId,
        User = null!,
        Provider = RepositoryBackedAtprotoSession.Provider,
        SubjectDid = Did,
        SessionCiphertext = [1],
        EncryptionKeyId = "enc-1",
        OAuthClientKeyId = "oauth-1",
        EnvelopeVersion = 1,
        PdsHost = "https://pds.example.test/"
    };

    private static UserExternalLogin LinkedLogin() => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = TenantId,
        Tenant = null!,
        UserId = UserId,
        User = null!,
        Provider = RepositoryBackedAtprotoSession.Provider,
        ProviderKey = Did
    };

    private static PdsSyncOutbox UnsettledCreate(
        PdsSyncStatus status,
        DateTime? leaseExpiresAt,
        PdsSyncOperation operation = PdsSyncOperation.Create,
        DateTime? nextRetryAt = null) => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = TenantId,
            UserId = UserId,
            Did = Did,
            Collection = AtprotoEventPublicationPlanner.EventCollection,
            RecordKey = "stable-event-key",
            Operation = operation,
            Payload = operation == PdsSyncOperation.Delete ? null : Payload,
            PayloadHash = PayloadHash,
            IdempotencyKey = "prior-create",
            PdsHost = "https://pds.example.test/",
            SourceEntityType = AtprotoEventPublicationPlanner.EventSourceType,
            SourceEntityId = EventId,
            SourceVersion = Guid.CreateVersion7(),
            Status = status,
            CreatedAt = CreatedAt.AddMinutes(-1),
            NextRetryAt = nextRetryAt,
            LeaseExpiresAt = leaseExpiresAt,
            LeaseOwner = status == PdsSyncStatus.Processing ? "worker" : null,
            LeaseToken = status == PdsSyncStatus.Processing ? Guid.CreateVersion7() : null,
            MaxRetries = 10
        };

    private static AtprotoEventPublicationEntityGraph Graph()
    {
        var tenant = new Tenant { FullName = "Tenant", Slug = "tenant", TenantStatus = null! };
        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            TenantId = TenantId,
            Tenant = tenant,
            ActorType = new ActorType { Id = 1, MasterCode = "USER", FullName = "User" },
            Pii = new ActorPii { DisplayName = "Publisher" }
        };
        var eventEntity = new Explore.Domain.Event
        {
            Id = EventId,
            TenantId = TenantId,
            Tenant = tenant,
            ActorId = actor.Id,
            Actor = actor,
            Title = "Published",
            VisibilityTypeId = 1,
            VisibilityType = new VisibilityType { Id = 1, MasterCode = "PUBLIC", FullName = "Public" },
            EventStatusId = 2,
            EventStatus = new EventStatus { Id = 2, MasterCode = "PUBLISHED", FullName = "Published" },
            EventFormatId = 1,
            EventFormat = new EventFormat { Id = 1, MasterCode = "IN_PERSON", FullName = "In person" },
            ConcurrencyStamp = SourceVersion,
            CreatedAt = CreatedAt
        };
        return new(eventEntity, [], [], [], [], [], [], [], [], [], [], [], [], [], [], []);
    }

    private static IReadOnlyList<ResolvedSetting> ResolveSettings(
        IEnumerable<string> keys,
        bool enabled,
        bool consent) => keys.Select(key => new ResolvedSetting
        {
            Key = key,
            Value = key switch
            {
                GovernanceSettingKeys.Federation.AtprotoEventsEnabled => enabled ? "true" : "false",
                GovernanceSettingKeys.Federation.AtprotoEventValidationProfile => "\"platform\"",
                GovernanceSettingKeys.Federation.AtprotoPublishMyEvents => consent ? "true" : "false",
                _ => "false"
            },
            Source = SettingSource.SystemDefault
        }).ToArray();

    private sealed record PlannerFixture(
        AtprotoEventPublicationPlanner Planner,
        IHierarchicalSettingsResolver Settings,
        IEventRepository Events,
        IAtprotoRecordRepository Records,
        IUserAuthenticationTokenRepository Sessions,
        IUserExternalLoginRepository Logins,
        IAtprotoPublicationPayloadBuilder Payloads,
        IPdsSyncOutboxRepository Outbox);

    private sealed class CapturingLogger : ILogger<AtprotoEventPublicationPlanner>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Information)
            {
                Messages.Add(formatter(state, exception));
            }
        }
    }
}
