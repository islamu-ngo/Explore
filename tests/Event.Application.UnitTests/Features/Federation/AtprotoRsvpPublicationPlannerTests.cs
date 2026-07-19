// ABOUTME: Verifies governed RSVP outbox planning from committed attendee registration intent only.
// ABOUTME: Covers settled-event dependencies, going-only payloads, no synthetic remote RSVP, and last-registration deletion.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Services.Federation;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Federation;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Federation;

public sealed class AtprotoRsvpPublicationPlannerTests
{
    private static readonly Guid TenantId = Guid.Parse("0198ab00-0000-7000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("0198ab00-0000-7000-8000-000000000002");
    private static readonly Guid EventId = Guid.Parse("0198ab00-0000-7000-8000-000000000003");
    private static readonly Guid IntentId = Guid.Parse("0198ab00-0000-7000-8000-000000000004");
    private static readonly Guid Version = Guid.Parse("0198ab00-0000-7000-8000-000000000005");
    private static readonly Guid OutboxId = Guid.Parse("0198ab00-0000-7000-8000-000000000006");
    private static readonly DateTime CreatedAt = new(2026, 7, 19, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ActiveCommittedRegistration_EnqueuesGoingWithSettledEventDependency()
    {
        var fixture = Fixture();
        AtprotoRsvpPublicationSnapshot? captured = null;
        fixture.PayloadBuilder.BuildRsvp(Arg.Do<AtprotoRsvpPublicationSnapshot>(value => captured = value))
            .Returns(AtprotoPublicationPayloadBuildResult.Valid(new("{\"status\":\"going\"}", "hash")));

        var result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Status).IsEqualTo(AtprotoRsvpPublicationSnapshotFactory.GoingStatus);
        await Assert.That(captured.SubjectUri).IsEqualTo("at://did:plc:organizer/community.lexicon.calendar.event/event-key");
        await Assert.That(result.Outbox!.DependsOnAtprotoRecordId).IsEqualTo(fixture.EventRecord.Id);
        await Assert.That(result.Outbox.Collection).IsEqualTo(AtprotoEventPublicationPlanner.RsvpCollection);
        await Assert.That(result.Outbox.RecordKey.Length).IsEqualTo(64);
        await Assert.That(result.Outbox.RecordKey).IsNotEqualTo(OutboxId.ToString("N"));
    }

    [Test]
    public async Task ActiveRegistration_WhenEventHasNoSettledCid_DoesNotEnqueue()
    {
        var fixture = Fixture(eventSettled: false);

        var result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo("event_record_missing");
        await fixture.Outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Test]
    public async Task ActiveRegistration_WhenSameUserEventRsvpIsPending_DoesNotEnqueueDuplicate()
    {
        var fixture = Fixture();
        fixture.Outbox.HasActiveRsvpPublicationAsync(
                TenantId,
                UserId,
                EventId,
                AtprotoEventPublicationPlanner.RsvpSourceType,
                AtprotoEventPublicationPlanner.RsvpCollection,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo("publication_pending");
        await fixture.Outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Test]
    public async Task Reconciliation_WhenExactPayloadAlreadyDeadLettered_DoesNotHotLoop()
    {
        var fixture = Fixture();
        fixture.Outbox.HasTerminalRsvpPublicationAttemptAsync(
                TenantId,
                UserId,
                EventId,
                Version,
                PdsSyncOperation.Create,
                "hash",
                AtprotoEventPublicationPlanner.RsvpSourceType,
                AtprotoEventPublicationPlanner.RsvpCollection,
                Arg.Any<CancellationToken>())
            .Returns(true);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo("publication_failed");
        await fixture.Outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Test]
    public async Task Reconciliation_WhenEventStrongRefPayloadChanged_EnqueuesOneNewAttempt()
    {
        var fixture = Fixture();
        fixture.PayloadBuilder.BuildRsvp(Arg.Any<AtprotoRsvpPublicationSnapshot>())
            .Returns(AtprotoPublicationPayloadBuildResult.Valid(new("{\"cid\":\"bafy-event-2\"}", "hash-2")));
        fixture.Outbox.HasTerminalRsvpPublicationAttemptAsync(
                TenantId,
                UserId,
                EventId,
                Version,
                PdsSyncOperation.Create,
                "hash-2",
                AtprotoEventPublicationPlanner.RsvpSourceType,
                AtprotoEventPublicationPlanner.RsvpCollection,
                Arg.Any<CancellationToken>())
            .Returns(false);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(result.Outbox!.PayloadHash).IsEqualTo("hash-2");
        await fixture.Outbox.Received(1).SupersedePriorRsvpAsync(
            TenantId,
            UserId,
            EventId,
            AtprotoEventPublicationPlanner.RsvpCollection,
            OutboxId,
            CreatedAt,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Cancellation_WhenRemoteRsvpDoesNotExist_DoesNotSynthesizeRecord()
    {
        var fixture = Fixture(remoteRsvp: false, activeCount: 0, deletedIntent: true);

        var result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Delete),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo("remote_record_missing");
        await fixture.Outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Test]
    public async Task Cancellation_WhenAnotherRegistrationRemains_DoesNotDeleteRemoteRsvp()
    {
        var fixture = Fixture(remoteRsvp: true, activeCount: 1, deletedIntent: true);

        var result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Delete),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo("active_registration_remains");
        await fixture.Outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Test]
    public async Task CancellationOfLastRegistration_DeletesOnlyExistingOwnedRsvp()
    {
        var fixture = Fixture(eventSettled: false, remoteRsvp: true, activeCount: 0, deletedIntent: true);

        var result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Delete),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(result.Outbox!.Operation).IsEqualTo(PdsSyncOperation.Delete);
        await Assert.That(result.Outbox.RecordKey).IsEqualTo("rsvp-key");
        await Assert.That(result.Outbox.ExpectedCid).IsEqualTo("bafy-rsvp");
        await Assert.That(result.Outbox.Payload).IsNull();
        await Assert.That(result.Outbox.DependsOnAtprotoRecordId).IsNull();
    }

    [Test]
    public async Task CancellationRacingUnsettledCreate_EnqueuesDelayedSameIdentityDelete()
    {
        var fixture = Fixture(eventSettled: false, remoteRsvp: false, activeCount: 0, deletedIntent: true);
        var prior = new PdsSyncOutbox
        {
            Id = Guid.CreateVersion7(),
            TenantId = TenantId,
            UserId = UserId,
            Did = "did:plc:attendee",
            Collection = AtprotoEventPublicationPlanner.RsvpCollection,
            RecordKey = "stable-rsvp-key",
            Operation = PdsSyncOperation.Create,
            Payload = "{}",
            PayloadHash = "hash",
            IdempotencyKey = "prior-rsvp-create",
            PdsHost = "https://pds.example/",
            SourceEntityType = AtprotoEventPublicationPlanner.RsvpSourceType,
            SourceEntityId = IntentId,
            SourceVersion = Guid.CreateVersion7(),
            Status = PdsSyncStatus.Processing,
            CreatedAt = CreatedAt.AddMinutes(-1),
            LeaseOwner = "worker",
            LeaseToken = Guid.CreateVersion7(),
            LeaseExpiresAt = CreatedAt.AddSeconds(90),
            MaxRetries = 10
        };
        fixture.Outbox.GetLatestUnsettledRsvpMutationAsync(
                TenantId,
                UserId,
                EventId,
                AtprotoEventPublicationPlanner.RsvpSourceType,
                AtprotoEventPublicationPlanner.RsvpCollection,
                Arg.Any<CancellationToken>())
            .Returns(prior);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Delete),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(result.Outbox!.Operation).IsEqualTo(PdsSyncOperation.Delete);
        await Assert.That(result.Outbox.RecordKey).IsEqualTo(prior.RecordKey);
        await Assert.That(result.Outbox.ExpectedCid).IsNull();
        await Assert.That(result.Outbox.NextRetryAt).IsNotNull();
        await Assert.That(result.Outbox.NextRetryAt!.Value).IsGreaterThan(prior.LeaseExpiresAt!.Value);
    }

    [Test]
    public async Task ReregisterSupersedingDelayedCancellation_InheritsSafetyWindow()
    {
        var fixture = Fixture();
        DateTime inheritedSafetyWindow = CreatedAt.AddMinutes(3);
        var prior = new PdsSyncOutbox
        {
            Id = Guid.CreateVersion7(),
            TenantId = TenantId,
            UserId = UserId,
            Did = "did:plc:attendee",
            Collection = AtprotoEventPublicationPlanner.RsvpCollection,
            RecordKey = "stable-rsvp-key",
            Operation = PdsSyncOperation.Delete,
            PayloadHash = new string('0', 64),
            IdempotencyKey = "prior-rsvp-delete",
            PdsHost = "https://pds.example/",
            SourceEntityType = AtprotoEventPublicationPlanner.RsvpSourceType,
            SourceEntityId = Guid.CreateVersion7(),
            SourceVersion = Guid.CreateVersion7(),
            Status = PdsSyncStatus.Pending,
            CreatedAt = CreatedAt.AddMinutes(-1),
            NextRetryAt = inheritedSafetyWindow,
            MaxRetries = 10
        };
        fixture.Outbox.GetLatestUnsettledRsvpMutationAsync(
                TenantId,
                UserId,
                EventId,
                AtprotoEventPublicationPlanner.RsvpSourceType,
                AtprotoEventPublicationPlanner.RsvpCollection,
                Arg.Any<CancellationToken>())
            .Returns(prior);

        AtprotoPublicationPlanningResult result = await fixture.Planner.PlanRsvpAsync(
            Request(PdsSyncOperation.Create),
            CancellationToken.None);

        await Assert.That(result.Enqueued).IsTrue();
        await Assert.That(result.Outbox!.Operation).IsEqualTo(PdsSyncOperation.Update);
        await Assert.That(result.Outbox.RecordKey).IsEqualTo(prior.RecordKey);
        await Assert.That(result.Outbox.NextRetryAt).IsEqualTo(inheritedSafetyWindow);
    }

    private static FixtureState Fixture(
        bool eventSettled = true,
        bool remoteRsvp = false,
        int activeCount = 1,
        bool deletedIntent = false)
    {
        var settings = Substitute.For<IHierarchicalSettingsResolver>();
        settings.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IEnumerable<string>>().Select(key => new ResolvedSetting
            {
                Key = key,
                Value = key == GovernanceSettingKeys.Federation.AtprotoEventValidationProfile
                    ? "\"platform\""
                    : "true",
                Source = SettingSource.UserPreference
            }).ToArray());
        var intents = Substitute.For<IEventRegistrationIntentRepository>();
        var intent = Intent(deletedIntent);
        intents.GetAtprotoLifecycleStateAsync(TenantId, IntentId, Arg.Any<CancellationToken>()).Returns(intent);
        intents.CountActiveForEventUserAsync(TenantId, EventId, UserId, Arg.Any<CancellationToken>()).Returns(activeCount);
        var records = Substitute.For<IAtprotoRecordRepository>();
        var eventRecord = EventRecord(eventSettled);
        records.GetOwnedRecordForSourceAsync(TenantId, AtprotoEventPublicationPlanner.EventSourceType, EventId, Arg.Any<CancellationToken>())
            .Returns(new AtprotoOutboundRecordOwnership
            {
                AtprotoRecordId = eventRecord.Id,
                TenantId = TenantId,
                UserId = Guid.CreateVersion7(),
                SourceEntityType = AtprotoEventPublicationPlanner.EventSourceType,
                SourceEntityId = EventId,
                SourceVersion = Guid.CreateVersion7(),
                AtprotoRecord = eventRecord
            });
        records.GetOwnedRsvpForUserEventAsync(
                TenantId,
                UserId,
                EventId,
                AtprotoEventPublicationPlanner.RsvpSourceType,
                AtprotoEventPublicationPlanner.RsvpCollection,
                Arg.Any<CancellationToken>())
            .Returns(remoteRsvp ? RsvpOwnership(intent) : null);
        var sessions = Substitute.For<IUserAuthenticationTokenRepository>();
        sessions.GetAtprotoSessionsForReadAsync(TenantId, UserId, RepositoryBackedAtprotoSession.Provider, Arg.Any<CancellationToken>())
            .Returns([Session()]);
        var logins = Substitute.For<IUserExternalLoginRepository>();
        logins.GetByProviderAndKey(RepositoryBackedAtprotoSession.Provider, "did:plc:attendee")
            .Returns(new UserExternalLogin
            {
                Id = Guid.CreateVersion7(),
                TenantId = TenantId,
                Tenant = null!,
                UserId = UserId,
                User = null!,
                Provider = RepositoryBackedAtprotoSession.Provider,
                ProviderKey = "did:plc:attendee"
            });
        var payloadBuilder = Substitute.For<IAtprotoPublicationPayloadBuilder>();
        payloadBuilder.BuildRsvp(Arg.Any<AtprotoRsvpPublicationSnapshot>())
            .Returns(AtprotoPublicationPayloadBuildResult.Valid(new("{}", "hash")));
        var outbox = Substitute.For<IPdsSyncOutboxRepository>();
        var planner = new AtprotoEventPublicationPlanner(
            new AtprotoEventGovernanceResolver(settings),
            Substitute.For<IEventRepository>(),
            intents,
            records,
            sessions,
            logins,
            payloadBuilder,
            outbox,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AtprotoEventPublicationPlanner>.Instance);
        return new(planner, outbox, payloadBuilder, eventRecord);
    }

    private static AtprotoRsvpPublicationInput Request(PdsSyncOperation operation) => new(
        TenantId,
        UserId,
        EventId,
        IntentId,
        Version,
        operation,
        OutboxId,
        CreatedAt);

    private static EventRegistrationIntent Intent(bool deleted) => new()
    {
        Id = IntentId,
        TenantId = TenantId,
        Tenant = null!,
        EventId = EventId,
        Event = null!,
        UserId = UserId,
        User = null!,
        RegistrationScopeId = 1,
        RegistrationScope = null!,
        ApprovalStatusId = 999,
        CreatedAt = CreatedAt,
        ConcurrencyStamp = Version,
        IsDeleted = deleted,
        DeletedAt = deleted ? CreatedAt : null
    };

    private static AtprotoRecord EventRecord(bool settled) => new()
    {
        Id = Guid.Parse("0198ab00-0000-7000-8000-000000000007"),
        Did = "did:plc:organizer",
        Collection = AtprotoEventPublicationPlanner.EventCollection,
        RecordKey = "event-key",
        Uri = settled ? "at://did:plc:organizer/community.lexicon.calendar.event/event-key" : null,
        Cid = settled ? "bafy-event" : null,
        UpdatedAt = CreatedAt
    };

    private static AtprotoOutboundRecordOwnership RsvpOwnership(EventRegistrationIntent intent)
    {
        var record = new AtprotoRecord
        {
            Id = Guid.Parse("0198ab00-0000-7000-8000-000000000008"),
            Did = "did:plc:attendee",
            Collection = AtprotoEventPublicationPlanner.RsvpCollection,
            RecordKey = "rsvp-key",
            Uri = "at://did:plc:attendee/community.lexicon.calendar.rsvp/rsvp-key",
            Cid = "bafy-rsvp",
            UpdatedAt = CreatedAt
        };
        return new AtprotoOutboundRecordOwnership
        {
            AtprotoRecordId = record.Id,
            TenantId = TenantId,
            UserId = UserId,
            SourceEntityType = AtprotoEventPublicationPlanner.RsvpSourceType,
            SourceEntityId = intent.Id,
            SourceVersion = intent.ConcurrencyStamp,
            AtprotoRecord = record
        };
    }

    private static UserAuthenticationToken Session() => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = TenantId,
        Tenant = null!,
        UserId = UserId,
        User = null!,
        Provider = RepositoryBackedAtprotoSession.Provider,
        SubjectDid = "did:plc:attendee",
        SessionCiphertext = [1],
        EncryptionKeyId = "enc",
        OAuthClientKeyId = "oauth",
        PdsHost = "https://pds.example/"
    };

    private sealed record FixtureState(
        AtprotoEventPublicationPlanner Planner,
        IPdsSyncOutboxRepository Outbox,
        IAtprotoPublicationPayloadBuilder PayloadBuilder,
        AtprotoRecord EventRecord);
}
