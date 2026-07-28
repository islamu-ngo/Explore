// ABOUTME: In-memory fallback tests for ATProto projection visibility when PostgreSQL containers are unavailable.
// ABOUTME: Exercises the exact Actor/DID and current-presentation correlation without bypassing tenant filters.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Event.Persistence.IntegrationTests.Federation;

public sealed class AtprotoEventProjectionVisibilityInMemoryTests
{
    [Test]
    public async Task VisibleQuery_ExcludesRowsWithoutTheExactCurrentActorIdentityCorrelation()
    {
        Guid tenantId = Guid.CreateVersion7();
        await using ExploreDbContext context = CreateContext(tenantId);
        Guid valid = AddProjection(context, tenantId, "valid");
        Guid suspendedActor = AddProjection(
            context,
            tenantId,
            "actor-suspended",
            configureEventActor: actor => actor.IsSuspended = true);
        Guid inactiveIdentity = AddProjection(
            context,
            tenantId,
            "identity-inactive",
            configureIdentity: identity => identity.IsActive = false);
        Guid didMismatch = AddProjection(
            context,
            tenantId,
            "did-mismatch",
            identityDid: "did:plc:not-the-record-did");
        Guid actorMismatch = AddProjection(
            context,
            tenantId,
            "actor-mismatch",
            identityActor: Actor("other-actor"));
        Guid stalePresentation = AddProjection(
            context,
            tenantId,
            "presentation-stale",
            presentationSourceVersion: 1);
        await context.SaveChangesAsync();
        var repository = new AtprotoEventProjectionRepository(context);

        (IReadOnlyList<AtprotoEventProjection> discovery, int totalCount) =
            await repository.GetPublicWindowAsync(PublicQuery(), CancellationToken.None);
        IReadOnlyList<AtprotoEventProjection> echoes = await repository.GetVisibleByRecordIdsAsync(
            [valid, suspendedActor, inactiveIdentity, didMismatch, actorMismatch, stalePresentation],
            CancellationToken.None);

        await Assert.That(discovery).HasSingleItem();
        await Assert.That(discovery[0].AtprotoRecordId).IsEqualTo(valid);
        await Assert.That(totalCount).IsEqualTo(1);
        await Assert.That(echoes).HasSingleItem();
        await Assert.That(echoes[0].AtprotoRecordId).IsEqualTo(valid);
    }

    [Test]
    public async Task VisibleQuery_AppliesImportedEventVisibilityAndLifecycleWithoutRequiringPublished()
    {
        Guid tenantId = Guid.CreateVersion7();
        await using ExploreDbContext context = CreateContext(tenantId);
        Guid draft = AddProjection(context, tenantId, "draft", eventStatus: EventStatusEnum.Draft);
        Guid published = AddProjection(context, tenantId, "published", eventStatus: EventStatusEnum.Published);
        Guid cancelled = AddProjection(context, tenantId, "cancelled", eventStatus: EventStatusEnum.Cancelled);
        Guid completed = AddProjection(context, tenantId, "completed", eventStatus: EventStatusEnum.Completed);
        Guid moderated = AddProjection(context, tenantId, "moderated", eventStatus: EventStatusEnum.Moderated);
        Guid archived = AddProjection(context, tenantId, "archived", eventStatus: EventStatusEnum.Archived);
        Guid deleted = AddProjection(context, tenantId, "deleted", eventIsDeleted: true);
        Guid privateEvent = AddProjection(context, tenantId, "private", visibility: VisibilityTypeEnum.Private);
        Guid unlisted = AddProjection(context, tenantId, "unlisted", visibility: VisibilityTypeEnum.Unlisted);
        Guid membersOnly = AddProjection(context, tenantId, "members", visibility: VisibilityTypeEnum.MembersOnly);
        Guid detached = AddProjection(context, tenantId, "detached", detachRecordAnchor: true);
        await context.SaveChangesAsync();
        var repository = new AtprotoEventProjectionRepository(context);

        (IReadOnlyList<AtprotoEventProjection> window, int totalCount) =
            await repository.GetPublicWindowAsync(PublicQuery(), CancellationToken.None);
        IReadOnlyList<AtprotoEventProjection> exact = await repository.GetVisibleByRecordIdsAsync(
            [draft, published, cancelled, completed, moderated, archived, deleted, privateEvent, unlisted, membersOnly, detached],
            CancellationToken.None);

        await Assert.That(window).Count().IsEqualTo(3);
        await Assert.That(window.Any(value => value.AtprotoRecordId == draft)).IsTrue();
        await Assert.That(window.Any(value => value.AtprotoRecordId == cancelled)).IsTrue();
        await Assert.That(window.Any(value => value.AtprotoRecordId == completed)).IsTrue();
        await Assert.That(window.Any(value => value.AtprotoRecordId == published)).IsFalse();
        await Assert.That(totalCount).IsEqualTo(3);
        await Assert.That(exact).Count().IsEqualTo(4);
        await Assert.That(exact.Any(value => value.AtprotoRecordId == draft)).IsTrue();
        await Assert.That(exact.Any(value => value.AtprotoRecordId == published)).IsTrue();
        await Assert.That(exact.Any(value => value.AtprotoRecordId == cancelled)).IsTrue();
        await Assert.That(exact.Any(value => value.AtprotoRecordId == completed)).IsTrue();

        foreach (Guid deniedRecordId in new[] { moderated, archived, deleted, privateEvent, unlisted, membersOnly, detached })
        {
            await Assert.That(exact.Any(value => value.AtprotoRecordId == deniedRecordId)).IsFalse();
            await Assert.That(await repository.GetVisibleByRecordIdAsync(deniedRecordId, CancellationToken.None)).IsNull();
        }
    }

    private static ExploreDbContext CreateContext(Guid tenantId) => new(
        new DbContextOptionsBuilder<ExploreDbContext>()
            .UseInMemoryDatabase($"atproto-projection-visibility-{Guid.NewGuid():N}")
            .Options)
    {
        TenantContext = new TestTenantContext(tenantId)
    };

    private static Guid AddProjection(
        ExploreDbContext context,
        Guid tenantId,
        string key,
        Action<Actor>? configureEventActor = null,
        Action<AtprotoIdentity>? configureIdentity = null,
        string? identityDid = null,
        Actor? identityActor = null,
        long presentationSourceVersion = 2,
        EventStatusEnum eventStatus = EventStatusEnum.Draft,
        VisibilityTypeEnum visibility = VisibilityTypeEnum.Public,
        bool eventIsDeleted = false,
        bool detachRecordAnchor = false)
    {
        Actor eventActor = Actor($"event-owner-{key}");
        configureEventActor?.Invoke(eventActor);
        string recordDid = $"did:plc:in-memory-{key}";
        var record = new AtprotoRecord
        {
            Id = Guid.CreateVersion7(),
            Did = recordDid,
            Collection = "community.lexicon.calendar.event",
            RecordKey = $"record-{key}",
            Direction = AtprotoRecordDirection.Inbound,
            Provenance = AtprotoRecordProvenance.Jetstream,
            SourceVersion = 2,
            RecordJson = "{\"name\":\"In-memory visibility test\"}",
            RecordHash = new string('a', 64),
            UpdatedAt = DateTime.UtcNow
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
            LastResolvedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        configureIdentity?.Invoke(identity);
        var projection = new AtprotoEventProjection
        {
            AtprotoRecordId = record.Id,
            Name = $"In-memory {key}",
            CreatedAt = DateTimeOffset.UtcNow,
            StartsAt = DateTimeOffset.UtcNow.AddDays(1),
            SourceVersion = 2,
            MaterializedAt = DateTime.UtcNow
        };
        var presentation = new AtprotoRecordTenantPresentation
        {
            TenantId = tenantId,
            AtprotoRecordId = record.Id,
            IsVisible = true,
            SourceVersion = presentationSourceVersion,
            EvaluatedAt = DateTime.UtcNow
        };
        var importedEvent = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            Title = $"In-memory {key}",
            PublicCode = "ATPROTO",
            ActorId = eventActor.Id,
            Actor = eventActor,
            TenantId = tenantId,
            Tenant = null!,
            VisibilityTypeId = (int)visibility,
            VisibilityType = null!,
            EventStatusId = (int)eventStatus,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Digital,
            EventFormat = null!,
            AtprotoRecordId = detachRecordAnchor ? null : record.Id,
            AtprotoRecord = detachRecordAnchor ? null : record,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = eventIsDeleted,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        context.AddRange(identity, projection, presentation, importedEvent);
        return record.Id;
    }

    private static Actor Actor(string displayName) => new()
    {
        Id = Guid.CreateVersion7(),
        ActorTypeId = (int)ActorTypeEnum.Bot,
        ActorType = null!,
        Pii = new ActorPii { DisplayName = displayName },
        CreatedAt = DateTime.UtcNow,
        ConcurrencyStamp = Guid.CreateVersion7()
    };

    private static AtprotoEventProjectionQuery PublicQuery() => new(
        20,
        null,
        null,
        null,
        null,
        AtprotoEventTemporalFilter.All,
        AtprotoEventDiscoverySort.Date,
        false,
        DateTimeOffset.UtcNow);

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}
