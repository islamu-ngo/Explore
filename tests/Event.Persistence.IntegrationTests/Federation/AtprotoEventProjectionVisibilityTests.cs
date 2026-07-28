// ABOUTME: PostgreSQL integration tests for public ATProto projection moderation and identity visibility.
// ABOUTME: Proves discovery and source reads share the same tenant, record, Actor, and exact DID safeguards.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Explore.Persistence;
using Explore.Persistence.Repositories;

namespace Event.Persistence.IntegrationTests.Federation;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class AtprotoEventProjectionVisibilityTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task VisibleQuery_RequiresCurrentTenantPresentationAndExactActiveActorIdentity()
    {
        await fixture.ResetAsync();
        VisibilitySeed seed = await SeedAsync();
        await using ExploreDbContext context =
            fixture.CreateTenantFilteredDbContext(new StaticTenantContext(seed.TenantId));
        var repository = new AtprotoEventProjectionRepository(context);

        (IReadOnlyList<AtprotoEventProjection> discovery, int totalCount) =
            await repository.GetPublicWindowAsync(PublicQuery(), CancellationToken.None);
        IReadOnlyList<AtprotoEventProjection> echoes = await repository.GetVisibleByRecordIdsAsync(
            seed.AllRecordIds,
            CancellationToken.None);
        AtprotoEventProjection? source = await repository.GetVisibleByRecordIdAsync(
            seed.VisibleRecordId,
            CancellationToken.None);

        await Assert.That(discovery).HasSingleItem();
        await Assert.That(discovery[0].AtprotoRecordId).IsEqualTo(seed.VisibleRecordId);
        await Assert.That(totalCount).IsEqualTo(1);
        await Assert.That(echoes).HasSingleItem();
        await Assert.That(echoes[0].AtprotoRecordId).IsEqualTo(seed.VisibleRecordId);
        await Assert.That(source).IsNotNull();
        await Assert.That(source!.AtprotoRecordId).IsEqualTo(seed.VisibleRecordId);

        foreach (Guid deniedRecordId in seed.DeniedRecordIds)
        {
            AtprotoEventProjection? denied = await repository.GetVisibleByRecordIdAsync(
                deniedRecordId,
                CancellationToken.None);
            await Assert.That(denied).IsNull();
        }
    }

    private async Task<VisibilitySeed> SeedAsync()
    {
        DateTime now = new(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc);
        var tenant = Tenant("visibility-a");
        var otherTenant = Tenant("visibility-b");
        await using ExploreDbContext context = fixture.CreateDbContext();
        context.AddRange(tenant, otherTenant);

        Guid visible = AddProjectionGraph(context, tenant, tenant, "valid", now);
        Guid suspendedActor = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "actor-suspended",
            now,
            configureEventActor: actor => actor.IsSuspended = true);
        Guid deletedActor = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "actor-deleted",
            now,
            configureEventActor: actor => actor.IsDeleted = true);
        Guid inactiveIdentity = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "identity-inactive",
            now,
            configureIdentity: identity => identity.IsActive = false);
        Guid suspendedIdentity = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "identity-suspended",
            now,
            configureIdentity: identity => identity.IsSuspended = true);
        Guid deletedIdentity = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "identity-deleted",
            now,
            configureIdentity: identity => identity.IsDeleted = true);
        Guid didMismatch = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "did-mismatch",
            now,
            identityDid: "did:plc:identity-does-not-match-record");
        Guid actorMismatch = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "actor-mismatch",
            now,
            identityActor: Actor("identity-owner", now));
        Guid hiddenPresentation = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "presentation-hidden",
            now,
            isVisible: false);
        Guid stalePresentation = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "presentation-stale",
            now,
            presentationSourceVersion: 1);
        Guid crossTenantPresentation = AddProjectionGraph(
            context,
            tenant,
            otherTenant,
            "presentation-other-tenant",
            now);
        Guid tombstonedRecord = AddProjectionGraph(
            context,
            tenant,
            tenant,
            "record-tombstoned",
            now,
            tombstoned: true);
        await context.SaveChangesAsync();

        return new(
            tenant.Id,
            visible,
            [
                visible,
                suspendedActor,
                deletedActor,
                inactiveIdentity,
                suspendedIdentity,
                deletedIdentity,
                didMismatch,
                actorMismatch,
                hiddenPresentation,
                stalePresentation,
                crossTenantPresentation,
                tombstonedRecord
            ],
            [
                suspendedActor,
                deletedActor,
                inactiveIdentity,
                suspendedIdentity,
                deletedIdentity,
                didMismatch,
                actorMismatch,
                hiddenPresentation,
                stalePresentation,
                crossTenantPresentation,
                tombstonedRecord
            ]);
    }

    private static Guid AddProjectionGraph(
        ExploreDbContext context,
        Tenant eventTenant,
        Tenant presentationTenant,
        string key,
        DateTime now,
        Action<Actor>? configureEventActor = null,
        Action<AtprotoIdentity>? configureIdentity = null,
        string? identityDid = null,
        Actor? identityActor = null,
        bool isVisible = true,
        long presentationSourceVersion = 2,
        bool tombstoned = false)
    {
        Actor eventActor = Actor($"event-owner-{key}", now);
        configureEventActor?.Invoke(eventActor);
        string recordDid = $"did:plc:visibility-{key}";
        var record = new AtprotoRecord
        {
            Id = Guid.CreateVersion7(),
            Did = recordDid,
            Collection = "community.lexicon.calendar.event",
            RecordKey = $"record-{key}",
            Cid = $"bafy-visibility-{key}",
            Uri = $"at://{recordDid}/community.lexicon.calendar.event/record-{key}",
            Direction = AtprotoRecordDirection.Inbound,
            Provenance = AtprotoRecordProvenance.Jetstream,
            SourceVersion = 2,
            RecordJson = "{\"name\":\"Visibility test\"}",
            RecordHash = new string('a', 64),
            IndexedAt = now,
            UpdatedAt = now,
            TombstonedAt = tombstoned ? now : null
        };
        Actor identityOwner = identityActor ?? eventActor;
        var identity = new AtprotoIdentity
        {
            Id = Guid.CreateVersion7(),
            Did = identityDid ?? recordDid,
            ActorId = identityOwner.Id,
            Actor = identityOwner,
            PdsHost = "https://pds.example.test",
            IsActive = true,
            LastResolvedAt = now,
            LastSeenAt = now,
            CreatedAt = now
        };
        configureIdentity?.Invoke(identity);
        var projection = new AtprotoEventProjection
        {
            AtprotoRecordId = record.Id,
            Name = $"Visibility {key}",
            CreatedAt = new DateTimeOffset(now),
            StartsAt = new DateTimeOffset(now.AddDays(1)),
            SourceUrl = "https://events.example.test/source",
            SourceVersion = 2,
            MaterializedAt = now
        };
        var presentation = new AtprotoRecordTenantPresentation
        {
            TenantId = presentationTenant.Id,
            Tenant = presentationTenant,
            AtprotoRecordId = record.Id,
            AtprotoRecord = record,
            IsVisible = isVisible,
            SourceVersion = presentationSourceVersion,
            EvaluatedAt = now
        };
        var importedEvent = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            Title = $"Visibility {key}",
            PublicCode = "ATPROTO",
            ActorId = eventActor.Id,
            Actor = eventActor,
            TenantId = eventTenant.Id,
            Tenant = eventTenant,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Draft,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Digital,
            EventFormat = null!,
            AtprotoRecordId = record.Id,
            AtprotoRecord = record,
            CreatedAt = now,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        context.AddRange(identity, projection, presentation, importedEvent);
        return record.Id;
    }

    private static Actor Actor(string displayName, DateTime now)
    {
        var externalSubject = new ExternalActorSubject
        {
            Id = Guid.CreateVersion7(),
            FirstObservedAt = now,
            LastObservedAt = now,
            CreatedAt = now
        };
        return new()
        {
            Id = Guid.CreateVersion7(),
            ActorTypeId = (int)ActorTypeEnum.Bot,
            ActorType = null!,
            ExternalActorSubjectId = externalSubject.Id,
            ExternalActorSubject = externalSubject,
            Pii = new ActorPii { DisplayName = displayName },
            CreatedAt = now,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
    }

    private static Tenant Tenant(string slug) => new()
    {
        Id = Guid.CreateVersion7(),
        FullName = slug,
        Slug = $"{slug}-{Guid.CreateVersion7():N}",
        TenantStatusId = (int)TenantStatusEnum.Active,
        TenantStatus = null!
    };

    private static AtprotoEventProjectionQuery PublicQuery() => new(
        50,
        null,
        null,
        null,
        null,
        AtprotoEventTemporalFilter.All,
        AtprotoEventDiscoverySort.Date,
        false,
        new DateTimeOffset(2026, 7, 28, 10, 0, 0, TimeSpan.Zero));

    private sealed record VisibilitySeed(
        Guid TenantId,
        Guid VisibleRecordId,
        IReadOnlyCollection<Guid> AllRecordIds,
        IReadOnlyCollection<Guid> DeniedRecordIds);

    private sealed record StaticTenantContext(Guid TenantId) : ITenantContext;
}
