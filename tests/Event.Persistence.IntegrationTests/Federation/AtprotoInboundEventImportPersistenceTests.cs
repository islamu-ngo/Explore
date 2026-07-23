// ABOUTME: PostgreSQL acceptance tests for importing canonical inbound AT Protocol events into local Event aggregates.
// ABOUTME: Proves canonical persistence, idempotent aggregate/session import, mapped updates, tombstones, and snapshot recovery.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Validators;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Explore.Persistence.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text;

namespace Event.Persistence.IntegrationTests.Federation;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class AtprotoInboundEventImportPersistenceTests(PostgreSqlContainerFixture fixture)
{
    private const string Did = "did:plc:remote-import-owner";
    private const string Collection = "community.lexicon.calendar.event";
    private const string RecordKey = "3msnapshota22";
    private const string Service = "https://jetstream.example/import";

    [Test]
    public async Task InboundRequestValidation_RejectsMalformedAndOversizedOptionalFields()
    {
        var validator = new AtprotoFederatedEventImportInputValidator();
        var input = new AtprotoFederatedEventImportInput(
            Name: "Unsafe import",
            CreatedAt: UtcOffset(10))
        {
            Description = new string('x', 4001),
            SourceUrl = $"https://events.example/{new string('x', 2049)}",
            StartsAt = null,
            EndsAt = UtcOffset(14),
            Mode = "#unsupported",
            Status = "#unsupported"
        };

        var result = await validator.ValidateAsync(input);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(AtprotoFederatedEventImportInput.Description))).IsTrue();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(AtprotoFederatedEventImportInput.SourceUrl))).IsTrue();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(AtprotoFederatedEventImportInput.EndsAt))).IsTrue();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(AtprotoFederatedEventImportInput.Mode))).IsTrue();
        await Assert.That(result.Errors.Any(error =>
            error.PropertyName == nameof(AtprotoFederatedEventImportInput.Status))).IsTrue();
    }

    [Test]
    public async Task JetstreamApply_PersistsCanonicalProjectionAndTenantPresentation()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-pin");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord record = Record(1, observedAt, "Pinned event", "https://events.example/pinned");
        AtprotoEventProjection projection = Projection(
            record,
            "Pinned event",
            UtcOffset(10),
            UtcOffset(13),
            UtcOffset(14),
            "https://events.example/pinned",
            observedAt);

        bool applied = await repository.TryApplyAndAdvanceAsync(new(
            claim,
            ExpectedCursor: 0,
            NextCursor: 1,
            record,
            [Presentation(scope.TenantId)],
            Quarantine: null,
            observedAt,
            EventProjection: projection));

        context.ChangeTracker.Clear();
        AtprotoRecord canonical = await context.AtprotoRecords.AsNoTracking().SingleAsync();
        AtprotoEventProjection persistedProjection = await context.AtprotoEventProjections.AsNoTracking().SingleAsync();
        AtprotoRecordTenantPresentation presentation = await context.AtprotoRecordTenantPresentations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync();
        long cursor = await context.AtprotoJetstreamConsumerStates.AsNoTracking()
            .Select(value => value.Cursor)
            .SingleAsync();

        await Assert.That(applied).IsTrue();
        await Assert.That(canonical.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(persistedProjection.AtprotoRecordId).IsEqualTo(canonical.Id);
        await Assert.That(persistedProjection.Name).IsEqualTo("Pinned event");
        await Assert.That(presentation.TenantId).IsEqualTo(scope.TenantId);
        await Assert.That(presentation.AtprotoRecordId).IsEqualTo(canonical.Id);
        await Assert.That(presentation.IsVisible).IsTrue();
        await Assert.That(cursor).IsEqualTo(1);
    }

    [Test]
    public async Task JetstreamApply_AcceptedInboundEventCreatesEventAndSessionWithMappedFieldsWithoutOutboundEcho()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-create");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        DateTimeOffset sourceCreatedAt = UtcOffset(10);
        DateTimeOffset startsAt = UtcOffset(13);
        DateTimeOffset endsAt = UtcOffset(14);
        const string source = "https://events.example/original";
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord record = Record(1, observedAt, "Imported event", source);

        AtprotoEventProjection projection = Projection(
            record,
            "Imported event",
            sourceCreatedAt,
            startsAt,
            endsAt,
            source,
            observedAt);
        bool applied = await repository.TryApplyAndAdvanceAsync(new AtprotoJetstreamApplyRequest(
            claim,
            ExpectedCursor: 0,
            NextCursor: 1,
            record,
            [Presentation(scope.TenantId)],
            Quarantine: null,
            observedAt,
            EventProjection: projection)
        {
            EventImports = [ImportPlan(scope.TenantId, record, projection)]
        });

        context.ChangeTracker.Clear();
        int eventCount = await context.Events.CountAsync();
        int sessionCount = await context.EventSessions.CountAsync();
        await Assert.That(sessionCount).IsEqualTo(1);
        await Assert.That(eventCount).IsEqualTo(1);

        Explore.Domain.Event imported = await context.Events.AsNoTracking().SingleAsync();
        EventSession session = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(applied).IsTrue();
        await Assert.That(imported.AtprotoRecordId).IsEqualTo(record.Id);
        await Assert.That(imported.TenantId).IsEqualTo(scope.TenantId);
        await Assert.That(imported.ActorId).IsEqualTo(scope.ActorId);
        await Assert.That(imported.Title).IsEqualTo("Imported event");
        await Assert.That(imported.Content).IsEqualTo("Imported event description");
        await Assert.That(imported.Description).IsEqualTo("Imported event description");
        await Assert.That(imported.EventUrl).IsEqualTo(source);
        await Assert.That(imported.CreatedAt).IsEqualTo(sourceCreatedAt.UtcDateTime);
        await Assert.That(imported.EventFormatId).IsEqualTo((int)EventFormatEnum.Digital);
        await Assert.That(imported.EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await Assert.That(imported.IsRegistrationRequired).IsFalse();
        await Assert.That(imported.ProvenanceSource).IsEqualTo("atproto");
        await Assert.That(imported.ProvenanceExternalId).IsEqualTo(record.Uri);
        await Assert.That(session.EventId).IsEqualTo(imported.Id);
        await Assert.That(session.TenantId).IsEqualTo(scope.TenantId);
        await Assert.That(session.Title).IsEqualTo("Imported event");
        await Assert.That(session.Description).IsNull();
        await Assert.That(session.StartTime).IsEqualTo(startsAt);
        await Assert.That(session.EndTime).IsEqualTo(endsAt);
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Published);
        await Assert.That(session.CreatedAt).IsEqualTo(sourceCreatedAt.UtcDateTime);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    [Arguments(null, EventStatusEnum.Published, EventSessionStatusEnum.Published)]
    [Arguments("#scheduled", EventStatusEnum.Published, EventSessionStatusEnum.Published)]
    [Arguments("#rescheduled", EventStatusEnum.Published, EventSessionStatusEnum.Published)]
    [Arguments("#planned", EventStatusEnum.Draft, EventSessionStatusEnum.Draft)]
    [Arguments("#postponed", EventStatusEnum.Draft, EventSessionStatusEnum.Draft)]
    [Arguments("#cancelled", EventStatusEnum.Cancelled, EventSessionStatusEnum.Cancelled)]
    public async Task JetstreamApply_MapsApprovedStatusMatrix(
        string? status,
        EventStatusEnum expectedEventStatus,
        EventSessionStatusEnum expectedSessionStatus)
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync($"atproto-import-status-{status ?? "absent"}");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord record = Record(1, observedAt, "Status event", "https://events.example/status");
        AtprotoEventProjection projection = Projection(
            record,
            "Status event",
            UtcOffset(10),
            UtcOffset(13),
            UtcOffset(14),
            "https://events.example/status",
            observedAt);
        bool applied = await repository.TryApplyAndAdvanceAsync(new AtprotoJetstreamApplyRequest(
            claim,
            ExpectedCursor: 0,
            NextCursor: 1,
            record,
            [Presentation(scope.TenantId)],
            Quarantine: null,
            observedAt,
            EventProjection: projection)
        {
            EventImports = [ImportPlan(scope.TenantId, record, projection, status)]
        });

        context.ChangeTracker.Clear();
        Explore.Domain.Event imported = await context.Events.AsNoTracking().SingleAsync();
        EventSession session = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(applied).IsTrue();
        await Assert.That(imported.EventStatusId).IsEqualTo((int)expectedEventStatus);
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)expectedSessionStatus);
        await Assert.That(await context.Events.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.EventSessions.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_StoresFullDescriptionAndUnicodeSafeScalarSummaryOnlyOnEvent()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-unicode-description");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord record = Record(1, observedAt, "Unicode event", "https://events.example/unicode");
        AtprotoEventProjection projection = Projection(
            record,
            "Unicode event",
            UtcOffset(10),
            UtcOffset(13),
            UtcOffset(14),
            "https://events.example/unicode",
            observedAt);
        string description = $"{new string('a', 149)}😀{new string('b', 200)}";
        string expectedSummary = string.Concat(
            description.EnumerateRunes().Take(150).Select(rune => rune.ToString()));

        bool applied = await repository.TryApplyAndAdvanceAsync(new AtprotoJetstreamApplyRequest(
            claim,
            ExpectedCursor: 0,
            NextCursor: 1,
            record,
            [Presentation(scope.TenantId)],
            Quarantine: null,
            observedAt,
            EventProjection: projection)
        {
            EventImports = [ImportPlan(scope.TenantId, record, projection, description: description)]
        });

        context.ChangeTracker.Clear();
        Explore.Domain.Event imported = await context.Events.AsNoTracking().SingleAsync();
        EventSession session = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(applied).IsTrue();
        await Assert.That(imported.Content).IsEqualTo(description);
        await Assert.That(imported.Description).IsEqualTo(expectedSummary);
        await Assert.That(imported.Description!.EnumerateRunes().Count()).IsEqualTo(150);
        await Assert.That(imported.Description!.EndsWith("😀", StringComparison.Ordinal)).IsTrue();
        await Assert.That(session.Description).IsNull();
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_DuplicateReplayPreservesImportedEventAndSessionIds()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-replay");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord first = Record(1, observedAt, "Replay event", "https://events.example/replay");

        bool applied = await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            0,
            1,
            first,
            scope.TenantId,
            "Replay event",
            "https://events.example/replay",
            observedAt));
        context.ChangeTracker.Clear();
        int eventCount = await context.Events.CountAsync();
        int sessionCount = await context.EventSessions.CountAsync();
        await Assert.That(eventCount).IsEqualTo(1);
        await Assert.That(sessionCount).IsEqualTo(1);
        Guid eventId = await context.Events.Select(value => value.Id).SingleAsync();
        Guid sessionId = await context.EventSessions.Select(value => value.Id).SingleAsync();

        AtprotoRecord replay = Record(1, observedAt.AddSeconds(1), "Ignored replay", "https://events.example/ignored");
        bool replayed = await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            1,
            2,
            replay,
            scope.TenantId,
            "Ignored replay",
            "https://events.example/ignored",
            observedAt.AddSeconds(1)));

        context.ChangeTracker.Clear();
        await Assert.That(applied).IsTrue();
        await Assert.That(replayed).IsTrue();
        await Assert.That(await context.Events.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.EventSessions.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.Events.Select(value => value.Id).SingleAsync()).IsEqualTo(eventId);
        await Assert.That(await context.EventSessions.Select(value => value.Id).SingleAsync()).IsEqualTo(sessionId);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_EqualVersionReplayRepairsMissingAndSoftDeletedSessionWithoutOverwritingEvent()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-equal-repair");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord first = Record(1, observedAt, "Healthy event", "https://events.example/healthy");
        await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            0,
            1,
            first,
            scope.TenantId,
            "Healthy event",
            "https://events.example/healthy",
            observedAt));

        context.ChangeTracker.Clear();
        Explore.Domain.Event healthy = await context.Events.AsNoTracking().SingleAsync();
        Guid eventId = healthy.Id;
        string? healthyContent = healthy.Content;
        Guid originalSessionId = await context.EventSessions.Select(value => value.Id).SingleAsync();
        await context.EventSessions.ExecuteDeleteAsync();

        AtprotoRecord missingSessionReplay = Record(
            1,
            observedAt.AddSeconds(1),
            "Ignored replay",
            "https://events.example/ignored");
        bool missingRepaired = await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            1,
            2,
            missingSessionReplay,
            scope.TenantId,
            "Ignored replay",
            "https://events.example/ignored",
            observedAt.AddSeconds(1)));

        context.ChangeTracker.Clear();
        Explore.Domain.Event afterMissingRepair = await context.Events.AsNoTracking().SingleAsync();
        EventSession repaired = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(missingRepaired).IsTrue();
        await Assert.That(afterMissingRepair.Id).IsEqualTo(eventId);
        await Assert.That(afterMissingRepair.Title).IsEqualTo("Healthy event");
        await Assert.That(afterMissingRepair.Content).IsEqualTo(healthyContent);
        await Assert.That(repaired.Id).IsNotEqualTo(originalSessionId);
        await Assert.That(await context.Events.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.EventSessions.CountAsync()).IsEqualTo(1);

        Guid repairedSessionId = repaired.Id;
        await context.EventSessions
            .Where(value => value.Id == repairedSessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(value => value.IsDeleted, true)
                .SetProperty(value => value.DeletedAt, observedAt.AddSeconds(2)));
        AtprotoRecord softDeletedSessionReplay = Record(
            1,
            observedAt.AddSeconds(2),
            "Second ignored replay",
            "https://events.example/ignored-again");
        bool softDeletedRepaired = await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            2,
            3,
            softDeletedSessionReplay,
            scope.TenantId,
            "Second ignored replay",
            "https://events.example/ignored-again",
            observedAt.AddSeconds(2)));

        context.ChangeTracker.Clear();
        Explore.Domain.Event afterSoftDeleteRepair = await context.Events.AsNoTracking().SingleAsync();
        EventSession reactivated = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(softDeletedRepaired).IsTrue();
        await Assert.That(afterSoftDeleteRepair.Id).IsEqualTo(eventId);
        await Assert.That(afterSoftDeleteRepair.Title).IsEqualTo("Healthy event");
        await Assert.That(afterSoftDeleteRepair.Content).IsEqualTo(healthyContent);
        await Assert.That(reactivated.Id).IsEqualTo(repairedSessionId);
        await Assert.That(reactivated.IsDeleted).IsFalse();
        await Assert.That(reactivated.DeletedAt).IsNull();
        await Assert.That(await context.EventSessions.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_NewerSourceVersionUpdatesImportedEventAndSessionFields()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-update");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord first = Record(1, observedAt, "Original title", "https://events.example/original");
        await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            0,
            1,
            first,
            scope.TenantId,
            "Original title",
            "https://events.example/original",
            observedAt));
        context.ChangeTracker.Clear();
        int eventCount = await context.Events.CountAsync();
        int sessionCount = await context.EventSessions.CountAsync();
        await Assert.That(eventCount).IsEqualTo(1);
        await Assert.That(sessionCount).IsEqualTo(1);
        Guid eventId = await context.Events.Select(value => value.Id).SingleAsync();
        Guid sessionId = await context.EventSessions.Select(value => value.Id).SingleAsync();

        AtprotoRecord newer = Record(2, observedAt.AddSeconds(1), "Updated title", "https://events.example/updated");
        DateTimeOffset updatedStart = UtcOffset(15);
        DateTimeOffset updatedEnd = UtcOffset(16);
        AtprotoEventProjection updatedProjection = Projection(
            newer,
            "Updated title",
            UtcOffset(10),
            updatedStart,
            updatedEnd,
            "https://events.example/updated",
            observedAt.AddSeconds(1));
        bool updated = await repository.TryApplyAndAdvanceAsync(new AtprotoJetstreamApplyRequest(
            claim,
            ExpectedCursor: 1,
            NextCursor: 2,
            newer,
            [Presentation(scope.TenantId)],
            Quarantine: null,
            observedAt.AddSeconds(1),
            EventProjection: updatedProjection)
        {
            EventImports = [ImportPlan(scope.TenantId, newer, updatedProjection)]
        });

        context.ChangeTracker.Clear();
        Explore.Domain.Event imported = await context.Events.AsNoTracking().SingleAsync();
        EventSession session = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(updated).IsTrue();
        await Assert.That(imported.Id).IsEqualTo(eventId);
        await Assert.That(session.Id).IsEqualTo(sessionId);
        await Assert.That(imported.Title).IsEqualTo("Updated title");
        await Assert.That(imported.EventUrl).IsEqualTo("https://events.example/updated");
        await Assert.That(session.Title).IsEqualTo("Updated title");
        await Assert.That(session.StartTime).IsEqualTo(updatedStart);
        await Assert.That(session.EndTime).IsEqualTo(updatedEnd);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_StaleSourceCannotOverwriteCanonicalEventOrSession()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-stale");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord current = Record(2, observedAt, "Current title", "https://events.example/current");
        await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            0,
            1,
            current,
            scope.TenantId,
            "Current title",
            "https://events.example/current",
            observedAt));

        context.ChangeTracker.Clear();
        Explore.Domain.Event before = await context.Events.AsNoTracking().SingleAsync();
        EventSession sessionBefore = await context.EventSessions.AsNoTracking().SingleAsync();
        AtprotoRecord stale = Record(1, observedAt.AddSeconds(1), "Stale title", "https://events.example/stale");
        bool applied = await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            1,
            2,
            stale,
            scope.TenantId,
            "Stale title",
            "https://events.example/stale",
            observedAt.AddSeconds(1)));

        context.ChangeTracker.Clear();
        AtprotoRecord canonical = await context.AtprotoRecords.AsNoTracking().SingleAsync();
        Explore.Domain.Event after = await context.Events.AsNoTracking().SingleAsync();
        EventSession sessionAfter = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(applied).IsTrue();
        await Assert.That(canonical.SourceVersion).IsEqualTo(2);
        await Assert.That(after.Id).IsEqualTo(before.Id);
        await Assert.That(after.Title).IsEqualTo("Current title");
        await Assert.That(after.EventUrl).IsEqualTo("https://events.example/current");
        await Assert.That(sessionAfter.Id).IsEqualTo(sessionBefore.Id);
        await Assert.That(sessionAfter.Title).IsEqualTo("Current title");
        await Assert.That(await context.Events.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.EventSessions.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_ConcurrentSameCanonicalRequestsConvergeToOneEventAndSession()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-concurrent");
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim;
        await using (ExploreDbContext claimContext = fixture.CreateDbContext())
        {
            claim = await ClaimAsync(new AtprotoJetstreamRepository(claimContext), observedAt);
        }

        await using ExploreDbContext firstContext = fixture.CreateDbContext();
        await using ExploreDbContext secondContext = fixture.CreateDbContext();
        AtprotoRecord first = Record(1, observedAt, "Concurrent event", "https://events.example/concurrent");
        AtprotoRecord second = Record(1, observedAt, "Concurrent event", "https://events.example/concurrent");
        bool[] results = await Task.WhenAll(
            new AtprotoJetstreamRepository(firstContext).TryApplyAndAdvanceAsync(ApplyRequest(
                claim,
                0,
                1,
                first,
                scope.TenantId,
                "Concurrent event",
                "https://events.example/concurrent",
                observedAt)),
            new AtprotoJetstreamRepository(secondContext).TryApplyAndAdvanceAsync(ApplyRequest(
                claim,
                0,
                1,
                second,
                scope.TenantId,
                "Concurrent event",
                "https://events.example/concurrent",
                observedAt)));

        await using ExploreDbContext verifyContext = fixture.CreateDbContext();
        await Assert.That(results.Count(value => value)).IsEqualTo(1);
        await Assert.That(await verifyContext.AtprotoRecords.CountAsync()).IsEqualTo(1);
        await Assert.That(await verifyContext.Events.CountAsync()).IsEqualTo(1);
        await Assert.That(await verifyContext.EventSessions.CountAsync()).IsEqualTo(1);
        Guid canonicalId = await verifyContext.AtprotoRecords.Select(value => value.Id).SingleAsync();
        Guid eventCanonicalId = await verifyContext.Events
            .Select(value => value.AtprotoRecordId!.Value)
            .SingleAsync();
        Guid eventId = await verifyContext.Events.Select(value => value.Id).SingleAsync();
        Guid sessionEventId = await verifyContext.EventSessions.Select(value => value.EventId).SingleAsync();
        await Assert.That(eventCanonicalId).IsEqualTo(canonicalId);
        await Assert.That(sessionEventId).IsEqualTo(eventId);
        await Assert.That(await verifyContext.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_ScheduleShapesReuseExactlyOneImportedSession()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-schedule");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);

        AtprotoRecord unscheduledRecord = Record(
            1,
            observedAt,
            "Unscheduled event",
            "https://events.example/schedule");
        AtprotoJetstreamApplyRequest unscheduledRequest = ApplyRequest(
            claim,
            0,
            1,
            unscheduledRecord,
            scope.TenantId,
            "Unscheduled event",
            "https://events.example/schedule",
            observedAt);
        unscheduledRequest = unscheduledRequest with
        {
            EventImports =
            [
                unscheduledRequest.EventImports.Single() with
                {
                    StartsAt = null,
                    EndsAt = null
                }
            ]
        };
        await repository.TryApplyAndAdvanceAsync(unscheduledRequest);

        context.ChangeTracker.Clear();
        EventSession unscheduled = await context.EventSessions.AsNoTracking().SingleAsync();
        Guid sessionId = unscheduled.Id;
        await Assert.That(unscheduled.StartTime).IsNull();
        await Assert.That(unscheduled.EndTime).IsNull();

        AtprotoRecord openEndedRecord = Record(
            2,
            observedAt.AddSeconds(1),
            "Open-ended event",
            "https://events.example/schedule");
        AtprotoJetstreamApplyRequest openEndedRequest = ApplyRequest(
            claim,
            1,
            2,
            openEndedRecord,
            scope.TenantId,
            "Open-ended event",
            "https://events.example/schedule",
            observedAt.AddSeconds(1));
        openEndedRequest = openEndedRequest with
        {
            EventImports =
            [
                openEndedRequest.EventImports.Single() with
                {
                    EndsAt = null
                }
            ]
        };
        await repository.TryApplyAndAdvanceAsync(openEndedRequest);

        context.ChangeTracker.Clear();
        EventSession openEnded = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(openEnded.Id).IsEqualTo(sessionId);
        await Assert.That(openEnded.StartTime).IsEqualTo(UtcOffset(13));
        await Assert.That(openEnded.EndTime).IsNull();
        await Assert.That(openEnded.EndTimeType).IsEqualTo(SessionEndTimeType.OpenEnded);

        AtprotoRecord fixedRecord = Record(
            3,
            observedAt.AddSeconds(2),
            "Fixed event",
            "https://events.example/schedule");
        await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            2,
            3,
            fixedRecord,
            scope.TenantId,
            "Fixed event",
            "https://events.example/schedule",
            observedAt.AddSeconds(2)));

        context.ChangeTracker.Clear();
        EventSession fixedSession = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(fixedSession.Id).IsEqualTo(sessionId);
        await Assert.That(fixedSession.StartTime).IsEqualTo(UtcOffset(13));
        await Assert.That(fixedSession.EndTime).IsEqualTo(UtcOffset(14));
        await Assert.That(fixedSession.EndTimeType).IsEqualTo(SessionEndTimeType.Fixed);
        await Assert.That(await context.EventSessions.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_TombstoneSoftDeletesImportedEventAndSession()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-tombstone");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord first = Record(1, observedAt, "Deleted remotely", "https://events.example/deleted");
        await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            0,
            1,
            first,
            scope.TenantId,
            "Deleted remotely",
            "https://events.example/deleted",
            observedAt));
        context.ChangeTracker.Clear();
        int eventCount = await context.Events.CountAsync();
        int sessionCount = await context.EventSessions.CountAsync();
        await Assert.That(eventCount).IsEqualTo(1);
        await Assert.That(sessionCount).IsEqualTo(1);

        AtprotoRecord tombstone = Record(2, observedAt.AddSeconds(1), "Deleted remotely", "https://events.example/deleted");
        tombstone.Cid = null;
        tombstone.RecordJson = null;
        tombstone.RecordHash = null;
        tombstone.TombstonedAt = observedAt.AddSeconds(1);
        bool deleted = await repository.TryApplyAndAdvanceAsync(new(
            claim,
            ExpectedCursor: 1,
            NextCursor: 2,
            tombstone,
            Presentations: [],
            Quarantine: null,
            observedAt.AddSeconds(1)));

        context.ChangeTracker.Clear();
        Explore.Domain.Event imported = await context.Events
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .AsNoTracking()
            .SingleAsync();
        EventSession session = await context.EventSessions
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .AsNoTracking()
            .SingleAsync();
        await Assert.That(deleted).IsTrue();
        await Assert.That(imported.IsDeleted).IsTrue();
        await Assert.That(imported.DeletedAt).IsEqualTo(observedAt.AddSeconds(1));
        await Assert.That(session.IsDeleted).IsTrue();
        await Assert.That(session.DeletedAt).IsEqualTo(observedAt.AddSeconds(1));
        await Assert.That(await context.Events.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.EventSessions.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);

        AtprotoRecord equalReplay = Record(
            2,
            observedAt.AddSeconds(2),
            "Must remain deleted",
            "https://events.example/must-remain-deleted");
        bool replayed = await repository.TryApplyAndAdvanceAsync(ApplyRequest(
            claim,
            2,
            3,
            equalReplay,
            scope.TenantId,
            "Must remain deleted",
            "https://events.example/must-remain-deleted",
            observedAt.AddSeconds(2)));

        context.ChangeTracker.Clear();
        Explore.Domain.Event afterReplay = await context.Events
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .AsNoTracking()
            .SingleAsync();
        EventSession sessionAfterReplay = await context.EventSessions
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .AsNoTracking()
            .SingleAsync();
        await Assert.That(replayed).IsTrue();
        await Assert.That(afterReplay.IsDeleted).IsTrue();
        await Assert.That(afterReplay.DeletedAt).IsEqualTo(observedAt.AddSeconds(1));
        await Assert.That(sessionAfterReplay.IsDeleted).IsTrue();
        await Assert.That(sessionAfterReplay.DeletedAt).IsEqualTo(observedAt.AddSeconds(1));
        await Assert.That(await context.Events.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.EventSessions.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_CancelAfterSaveRollsBackCanonicalEventSessionAndCursor()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-cancel");
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim;
        await using (ExploreDbContext claimContext = fixture.CreateDbContext())
        {
            claim = await ClaimAsync(new AtprotoJetstreamRepository(claimContext), observedAt);
        }

        var interceptor = new CancelAfterSaveInterceptor();
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(interceptor)
            .Options;
        await using (var failingContext = new ExploreDbContext(options))
        {
            failingContext.EnableTenantFilterBypass("ATProto import cancellation rollback test.");
            AtprotoRecord record = Record(1, observedAt, "Cancelled event", "https://events.example/cancelled");
            bool cancelled = false;
            try
            {
                await new AtprotoJetstreamRepository(failingContext).TryApplyAndAdvanceAsync(ApplyRequest(
                    claim,
                    0,
                    1,
                    record,
                    scope.TenantId,
                    "Cancelled event",
                    "https://events.example/cancelled",
                    observedAt));
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            await Assert.That(cancelled).IsTrue();
            await Assert.That(interceptor.FailuresInjected).IsEqualTo(1);
        }

        await using ExploreDbContext verifyContext = fixture.CreateDbContext();
        await Assert.That(await verifyContext.AtprotoRecords.CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.AtprotoEventProjections.CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.AtprotoRecordTenantPresentations.IgnoreQueryFilters().CountAsync())
            .IsEqualTo(0);
        await Assert.That(await verifyContext.Events.CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.EventSessions.CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.AtprotoJetstreamConsumerStates.Select(value => value.Cursor).SingleAsync())
            .IsEqualTo(0);
    }

    [Test]
    public async Task JetstreamApply_ExpiredCommitFenceRollsBackCanonicalEventSessionPresentationAndCursor()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-expired-fence");
        DateTime expiredObservedAt = Utc(12);
        AtprotoJetstreamClaim claim;
        await using (ExploreDbContext claimContext = fixture.CreateDbContext())
        {
            claim = await ClaimAsync(new AtprotoJetstreamRepository(claimContext), expiredObservedAt);
        }

        await using (ExploreDbContext applyContext = fixture.CreateDbContext())
        {
            AtprotoRecord record = Record(
                1,
                expiredObservedAt,
                "Expired fence event",
                "https://events.example/expired-fence");
            bool applied = await new AtprotoJetstreamRepository(applyContext).TryApplyAndAdvanceAsync(ApplyRequest(
                claim,
                0,
                1,
                record,
                scope.TenantId,
                "Expired fence event",
                "https://events.example/expired-fence",
                expiredObservedAt));
            await Assert.That(applied).IsFalse();
        }

        await using ExploreDbContext verifyContext = fixture.CreateDbContext();
        await Assert.That(await verifyContext.AtprotoRecords.CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.AtprotoEventProjections.CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.AtprotoRecordTenantPresentations.IgnoreQueryFilters().CountAsync())
            .IsEqualTo(0);
        await Assert.That(await verifyContext.Events.CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.EventSessions.CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.AtprotoJetstreamConsumerStates.Select(value => value.Cursor).SingleAsync())
            .IsEqualTo(0);
        await Assert.That(await verifyContext.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task PdsSnapshotReconcile_AcceptedInboundEventCreatesEventAndSessionWithoutOutboundEcho()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-snapshot");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        DateTimeOffset sourceCreatedAt = UtcOffset(10);
        const string source = "https://events.example/snapshot";
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord record = Record(0, observedAt, "Recovered event", source);
        AtprotoEventProjection projection = Projection(
            record,
            "Recovered event",
            sourceCreatedAt,
            UtcOffset(13),
            UtcOffset(14),
            source,
            observedAt);
        var snapshot = new AtprotoPdsSnapshot(
            Did,
            [new(Collection, RecordKey)],
            [new(record, projection)]);
        var request = new AtprotoPdsSnapshotApplyRequest(
            claim,
            [Did],
            [snapshot],
            [scope.TenantId],
            SnapshotVersion: 200,
            ObservedAt: observedAt)
        {
            EventImports = [ImportPlan(scope.TenantId, record, projection)]
        };

        bool applied = await repository.TryReconcileAsync(request, CancellationToken.None);

        context.ChangeTracker.Clear();
        int eventCount = await context.Events.CountAsync();
        int sessionCount = await context.EventSessions.CountAsync();
        await Assert.That(eventCount).IsEqualTo(1);
        await Assert.That(sessionCount).IsEqualTo(1);
        Explore.Domain.Event imported = await context.Events.AsNoTracking().SingleAsync();
        EventSession session = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(applied).IsTrue();
        await Assert.That(imported.AtprotoRecordId).IsEqualTo(record.Id);
        await Assert.That(imported.Title).IsEqualTo("Recovered event");
        await Assert.That(imported.EventUrl).IsEqualTo(source);
        await Assert.That(imported.CreatedAt).IsEqualTo(sourceCreatedAt.UtcDateTime);
        await Assert.That(session.EventId).IsEqualTo(imported.Id);
        await Assert.That(session.CreatedAt).IsEqualTo(sourceCreatedAt.UtcDateTime);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task PdsSnapshotReconcile_CompleteAbsenceDeletesOnlyMatchingAggregateAndSession()
    {
        await fixture.ResetAsync();
        ImportScope importedScope = await SeedScopeAsync("atproto-import-absence");
        ImportScope localScope = await SeedScopeAsync("atproto-import-absence-local");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        DateTime observedAt = CurrentUtc();
        AtprotoJetstreamClaim claim = await ClaimAsync(repository, observedAt);
        AtprotoRecord record = Record(0, observedAt, "Absent event", "https://events.example/absence");
        AtprotoEventProjection projection = Projection(
            record,
            "Absent event",
            UtcOffset(10),
            UtcOffset(13),
            UtcOffset(14),
            "https://events.example/absence",
            observedAt);
        bool created = await repository.TryReconcileAsync(new AtprotoPdsSnapshotApplyRequest(
            claim,
            [Did],
            [new AtprotoPdsSnapshot(Did, [new(Collection, RecordKey)], [new(record, projection)])],
            [importedScope.TenantId],
            SnapshotVersion: 200,
            ObservedAt: observedAt)
        {
            EventImports = [ImportPlan(importedScope.TenantId, record, projection)]
        }, CancellationToken.None);

        var localEvent = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            TenantId = localScope.TenantId,
            Tenant = null!,
            ActorId = localScope.ActorId,
            Actor = null!,
            Title = "Unrelated local event",
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            EventTimeZoneId = "UTC",
            Timezone = "UTC"
        };
        var localSession = new EventSession
        {
            Id = Guid.CreateVersion7(),
            TenantId = localScope.TenantId,
            Tenant = null!,
            EventId = localEvent.Id,
            Event = localEvent,
            Title = "Unrelated local event",
            EventSessionStatusId = (int)EventSessionStatusEnum.Published
        };
        context.AddRange(localEvent, localSession);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Guid importedEventId = await context.Events
            .Where(value => value.AtprotoRecordId != null)
            .Select(value => value.Id)
            .SingleAsync();
        Guid importedSessionId = await context.EventSessions
            .Where(value => value.EventId == importedEventId)
            .Select(value => value.Id)
            .SingleAsync();

        DateTime absenceObservedAt = observedAt.AddSeconds(1);
        bool reconciled = await repository.TryReconcileAsync(new AtprotoPdsSnapshotApplyRequest(
            claim,
            [Did],
            [new AtprotoPdsSnapshot(Did, [], [])],
            [importedScope.TenantId],
            SnapshotVersion: 201,
            ObservedAt: absenceObservedAt),
            CancellationToken.None);

        context.ChangeTracker.Clear();
        Explore.Domain.Event imported = await context.Events
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .AsNoTracking()
            .SingleAsync(value => value.Id == importedEventId);
        EventSession importedSession = await context.EventSessions
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .AsNoTracking()
            .SingleAsync(value => value.Id == importedSessionId);
        Explore.Domain.Event remaining = await context.Events.AsNoTracking().SingleAsync();
        EventSession remainingSession = await context.EventSessions.AsNoTracking().SingleAsync();
        await Assert.That(created).IsTrue();
        await Assert.That(reconciled).IsTrue();
        await Assert.That(imported.TenantId).IsEqualTo(importedScope.TenantId);
        await Assert.That(imported.IsDeleted).IsTrue();
        await Assert.That(imported.DeletedAt).IsEqualTo(absenceObservedAt);
        await Assert.That(importedSession.IsDeleted).IsTrue();
        await Assert.That(importedSession.DeletedAt).IsEqualTo(absenceObservedAt);
        await Assert.That(remaining.Id).IsEqualTo(localEvent.Id);
        await Assert.That(remaining.TenantId).IsEqualTo(localScope.TenantId);
        await Assert.That(remainingSession.Id).IsEqualTo(localSession.Id);
        await Assert.That(remainingSession.TenantId).IsEqualTo(localScope.TenantId);
        await Assert.That(await context.Events.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.EventSessions.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.PdsSyncOutbox.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task SaveChanges_AddedAuditableEntityUsesDefaultStampWhenSourceTimestampIsAbsent()
    {
        await fixture.ResetAsync();
        ImportScope scope = await SeedScopeAsync("atproto-import-default-stamp");
        await using ExploreDbContext context = fixture.CreateDbContext();
        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            TenantId = scope.TenantId,
            Tenant = null!,
            ActorTypeId = (int)ActorTypeEnum.Bot,
            ActorType = null!,
            Pii = new ActorPii
            {
                DisplayName = "Default stamp actor",
                Did = "did:plc:default-stamp"
            }
        };
        DateTime beforeSave = DateTime.UtcNow;
        context.Actors.Add(actor);
        await context.SaveChangesAsync();
        DateTime afterSave = DateTime.UtcNow;

        context.ChangeTracker.Clear();
        DateTime persisted = await context.Actors
            .Where(value => value.Id == actor.Id)
            .Select(value => value.CreatedAt)
            .SingleAsync();
        await Assert.That(persisted).IsGreaterThanOrEqualTo(beforeSave);
        await Assert.That(persisted).IsLessThanOrEqualTo(afterSave);
    }

    private async Task<ImportScope> SeedScopeAsync(string slug)
    {
        await using ExploreDbContext context = fixture.CreateDbContext();
        DateTime now = Utc(9);
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = slug,
            Slug = $"{slug}-{Guid.CreateVersion7():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"{slug}@example.test",
                FirstName = "Remote",
                LastName = "Organizer"
            },
            EmailVerified = true,
            ConcurrencyStamp = Guid.CreateVersion7(),
            CreatedAt = now
        };
        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = user.Id,
            User = user,
            TenantId = tenant.Id,
            Tenant = tenant,
            Pii = new ActorPii
            {
                DisplayName = "Remote organizer",
                Did = Did
            },
            CreatedAt = now,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        var tenantUser = new TenantUser
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = tenant,
            UserId = user.Id,
            User = user,
            StatusId = (int)TenantUserStatusEnum.Active,
            JoinedAt = now,
            CreatedAt = now
        };
        context.AddRange(actor, tenantUser);
        await context.SaveChangesAsync();
        return new(tenant.Id, actor.Id);
    }

    private static async Task<AtprotoJetstreamClaim> ClaimAsync(
        AtprotoJetstreamRepository repository,
        DateTime observedAt) =>
        await repository.TryClaimAsync(
            Service,
            "import-worker",
            observedAt,
            TimeSpan.FromMinutes(5))
        ?? throw new InvalidOperationException("Jetstream claim was not acquired.");

    private static AtprotoJetstreamApplyRequest ApplyRequest(
        AtprotoJetstreamClaim claim,
        long expectedCursor,
        long nextCursor,
        AtprotoRecord record,
        Guid tenantId,
        string name,
        string source,
        DateTime observedAt)
    {
        AtprotoEventProjection projection = Projection(
            record,
            name,
            UtcOffset(10),
            UtcOffset(13),
            UtcOffset(14),
            source,
            observedAt);
        return new AtprotoJetstreamApplyRequest(
            claim,
            expectedCursor,
            nextCursor,
            record,
            [Presentation(tenantId)],
            Quarantine: null,
            observedAt,
            EventProjection: projection)
        {
            EventImports = [ImportPlan(tenantId, record, projection)]
        };
    }

    private static AtprotoRecord Record(
        long sourceVersion,
        DateTime observedAt,
        string name,
        string source) => new()
        {
            Id = Guid.CreateVersion7(),
            Did = Did,
            Collection = Collection,
            RecordKey = RecordKey,
            Direction = AtprotoRecordDirection.Inbound,
            Provenance = AtprotoRecordProvenance.Jetstream,
            Cid = $"bafy-import-{sourceVersion}",
            Uri = $"at://{Did}/{Collection}/{RecordKey}",
            SourceVersion = sourceVersion,
            SourceCursor = sourceVersion,
            RecordJson = $$"""
                {
                  "name": "{{name}}",
                  "createdAt": "2026-07-18T10:00:00Z",
                  "startsAt": "2026-07-18T13:00:00Z",
                  "endsAt": "2026-07-18T14:00:00Z",
                  "source": "{{source}}"
                }
                """,
            RecordHash = new string('a', 64),
            IndexedAt = observedAt,
            UpdatedAt = observedAt
        };

    private static AtprotoEventProjection Projection(
        AtprotoRecord record,
        string name,
        DateTimeOffset createdAt,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string source,
        DateTime observedAt) => new()
        {
            AtprotoRecordId = record.Id,
            Name = name,
            Description = $"{name} description",
            CreatedAt = createdAt,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Mode = "community.lexicon.calendar.event#virtual",
            Status = "community.lexicon.calendar.event#scheduled",
            SourceUrl = source,
            SourceVersion = record.SourceVersion,
            MaterializedAt = observedAt
        };

    private static AtprotoRecordTenantPresentation Presentation(Guid tenantId) => new()
    {
        TenantId = tenantId,
        IsVisible = true
    };

    private static AtprotoFederatedEventImportPlan ImportPlan(
        Guid tenantId,
        AtprotoRecord record,
        AtprotoEventProjection projection,
        string? status = "#scheduled",
        string? description = null) => new(
            tenantId,
            record.Id,
            record.Did,
            record.Uri!,
            projection.Name,
            projection.CreatedAt,
            description ?? projection.Description,
            projection.SourceUrl,
            projection.StartsAt,
            projection.EndsAt,
            "#virtual",
            status,
            projection.RsvpExpected);

    private static DateTime Utc(int hour) =>
        new(2026, 7, 18, hour, 0, 0, DateTimeKind.Utc);

    private static DateTime CurrentUtc()
    {
        DateTime now = DateTime.UtcNow;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }

    private static DateTimeOffset UtcOffset(int hour) =>
        new(Utc(hour));

    private sealed record ImportScope(Guid TenantId, Guid ActorId);

    private sealed class CancelAfterSaveInterceptor : SaveChangesInterceptor
    {
        private int _armed = 1;

        public int FailuresInjected { get; private set; }

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _armed, 0) == 0)
            {
                return ValueTask.FromResult(result);
            }

            FailuresInjected++;
            throw new OperationCanceledException("Simulated cancellation before transaction commit.");
        }
    }
}
