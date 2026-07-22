// ABOUTME: PostgreSQL proofs for the owner-bounded cross-tenant Private Home erasure query.
// ABOUTME: Prevents global account deletion from enumerating unrelated owners or non-Home locations.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Infrastructure.Services.Privacy;
using Explore.Persistence;
using Explore.Persistence.Privacy.ErasureAuthority;
using Explore.Persistence.Privacy.ErasureAuthority.Repositories;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Explore.Persistence.Seed;
using Event.Persistence.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Event.Persistence.IntegrationTests.Privacy;

[Category("EventLocationPrivacy")]
[ClassDataSource<GlobalLocationPrivacyPostgreSqlFixture>(Shared = SharedType.PerClass)]
[NotInParallel("PersistenceDb")]
public sealed class GlobalLocationPrivacyErasureTests(GlobalLocationPrivacyPostgreSqlFixture fixture)
{
    [Test]
    public async Task OwnerPrivateHomeQuery_ReturnsExactCrossTenantSetWithoutEnumeratingOtherRows()
    {
        await using var seedContext = fixture.CreateDbContext();

        var tenantA = CreateTenant("global-erasure-a");
        var tenantB = CreateTenant("global-erasure-b");
        var owner = CreateUser("global-erasure-owner");
        var unrelatedOwner = CreateUser("global-erasure-unrelated");
        seedContext.AddRange(tenantA, tenantB, owner, unrelatedOwner);
        await seedContext.SaveChangesAsync();

        var tenantAHome = CreatePrivateHome(tenantA.Id, owner.Id, "Owner home A");
        var tenantBHome = CreatePrivateHome(tenantB.Id, owner.Id, "Owner home B");
        var unrelatedHome = CreatePrivateHome(tenantA.Id, unrelatedOwner.Id, "Unrelated home");
        var nonHome = CreateNonHome(tenantB.Id, "Commercial venue");
        seedContext.Locations.AddRange(tenantAHome, tenantBHome, unrelatedHome, nonHome);
        await seedContext.SaveChangesAsync();

        await using var tenantAContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantA.Id));
        var repository = new LocationRepository(tenantAContext);
        await using (var transaction = await tenantAContext.Database.BeginTransactionAsync())
        {
            try
            {
                await tenantAContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE locations DROP CONSTRAINT ck_locations_owner_private_home");
                await tenantAContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE locations SET owner_user_id = {owner.Id} WHERE id = {nonHome.Id}");

                Guid[] ownerRowsWithoutKindBoundary = await tenantAContext.Locations
                    .IgnoreTenantFilter(TenantFilterBypassReasons.UserPrivacyErasure)
                    .Where(location => location.OwnerUserId == owner.Id)
                    .Select(location => location.Id)
                    .ToArrayAsync();
                List<Location> result =
                    await repository.GetOwnedPrivateHomesForGlobalErasureAsync(owner.Id);

                await Assert.That(ownerRowsWithoutKindBoundary)
                    .Contains(nonHome.Id);
                await Assert.That(result.Select(location => location.Id))
                    .IsEquivalentTo([tenantAHome.Id, tenantBHome.Id]);
                await Assert.That(result.Select(location => location.Id))
                    .DoesNotContain(unrelatedHome.Id);
                await Assert.That(result.Select(location => location.Id))
                    .DoesNotContain(nonHome.Id);
                await Assert.That(result.All(location => location.OwnerUserId == owner.Id))
                    .IsTrue();
                await Assert.That(result.All(location =>
                    location.LocationKindId == (int)LocationKindEnum.PrivateHome))
                    .IsTrue();
                await Assert.That(result.Select(location => location.TenantId))
                    .IsEquivalentTo([tenantA.Id, tenantB.Id]);
                await Assert.That(result.All(location => location.Pii is not null))
                    .IsTrue();
                await Assert.That(result.All(location =>
                    tenantAContext.Entry(location).State == EntityState.Unchanged))
                    .IsTrue();
                await Assert.That(result.All(location =>
                    tenantAContext.Entry(location.Pii!).State == EntityState.Unchanged))
                    .IsTrue();
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }

        tenantAContext.ChangeTracker.Clear();
        Guid? restoredOwner = await tenantAContext.Locations
            .IgnoreTenantFilter(TenantFilterBypassReasons.UserPrivacyErasure)
            .Where(location => location.Id == nonHome.Id)
            .Select(location => location.OwnerUserId)
            .SingleAsync();
        await Assert.That(restoredOwner).IsNull();
    }

    [Test]
    public async Task OwnerPrivateHomeQuery_RejectsEmptyOwnerIdBeforeDatabaseAccess()
    {
        await using var context = fixture.CreateTenantFilteredDbContext();
        var repository = new LocationRepository(context);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.GetOwnedPrivateHomesForGlobalErasureAsync(Guid.Empty));

        await Assert.That(exception!.ParamName).IsEqualTo("ownerUserId");
    }

    [Test]
    [Timeout(240_000)]
    public async Task AuthorityFirstRollback_SequenceZeroAndRestoredBehindReplay_AreIdempotent()
    {
        ErasureGraph graph;
        await using (var seedContext = fixture.CreateDbContext())
        {
            graph = await SeedErasureGraphAsync(seedContext);
            await InstallOutboxFailureTriggerAsync(seedContext);
        }

        await using PrivacyErasureAuthorityDbContext authorityContext = fixture.CreateAuthorityDbContext();
        var authority = new EfCorePrivacyErasureAuthorityRepository(authorityContext);
        try
        {
            await using var failingContext = fixture.CreateDbContext();
            await using ErasureRuntime failingRuntime = CreateRuntime(failingContext, authority);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                failingRuntime.Service.EraseUserAsync(graph.OwnerUserId, CancellationToken.None));
        }
        finally
        {
            await using var triggerContext = fixture.CreateDbContext();
            await RemoveOutboxFailureTriggerAsync(triggerContext);
        }

        PrivacyErasureIntent retained;
        await using (var rollbackContext = fixture.CreateDbContext())
        {
            Location[] homes = await rollbackContext.Locations
                .IgnoreQueryFilters()
                .Include(location => location.Pii)
                .Where(location => graph.LocationIds.Contains(location.Id))
                .OrderBy(location => location.Id)
                .ToArrayAsync();
            LocationRoom[] rooms = await rollbackContext.LocationRooms
                .IgnoreQueryFilters()
                .Where(room => graph.RoomIds.Contains(room.Id))
                .ToArrayAsync();

            await Assert.That(homes.All(home =>
                home.LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Active
                && home.OwnerUserId == graph.OwnerUserId
                && home.Pii is not null)).IsTrue();
            await Assert.That(homes.Select(home => home.FullName))
                .IsEquivalentTo(graph.HomeNames);
            await Assert.That(rooms.Single(room => room.Id == graph.RoomIds[0]).IsDeleted)
                .IsFalse();
            await Assert.That(rooms.Single(room => room.Id == graph.RoomIds[1]).IsDeleted)
                .IsTrue();
            await Assert.That(rooms.All(room => room.Description is not null)).IsTrue();
            await Assert.That(await rollbackContext.Users
                .IgnoreQueryFilters()
                .AnyAsync(user => user.Id == graph.OwnerUserId && !user.IsDeleted)).IsTrue();
            await Assert.That(await rollbackContext.UserPii
                .AnyAsync(pii => pii.UserId == graph.OwnerUserId)).IsTrue();
            await Assert.That(await rollbackContext.PrivacyErasureReplayCheckpoints.CountAsync())
                .IsEqualTo(0);
            await Assert.That(await rollbackContext.PrivacyErasureIntents.CountAsync())
                .IsEqualTo(0);
            await Assert.That(await rollbackContext.PrivacyErasureCounters.CountAsync())
                .IsEqualTo(0);
            await Assert.That(await rollbackContext.OutboxMessages.CountAsync(message =>
                message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
                || message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType))
                .IsEqualTo(0);
        }

        IReadOnlyList<PrivacyErasureIntent> pending =
            await authority.ReadAfterAsync(0, 10);
        await Assert.That(pending.Count).IsEqualTo(1);
        retained = pending.Single();
        await Assert.That(retained.SubjectId).IsEqualTo(graph.OwnerUserId);
        await Assert.That(retained.SubjectKind).IsEqualTo(PrivacyErasureSubjectKind.User);

        PrivacyErasureIntent duplicate = await authority.AppendAsync(
            new PrivacyErasureRequest(
                retained.IntentId,
                retained.SubjectKind,
                retained.SubjectId,
                retained.ReasonCode,
                retained.PolicyVersion));
        await Assert.That(duplicate.AuthoritySequence).IsEqualTo(retained.AuthoritySequence);
        await Assert.That((await authority.ReadAfterAsync(0, 10)).Count).IsEqualTo(1);

        await using (var replayContext = fixture.CreateDbContext())
        await using (ErasureRuntime replayRuntime = CreateRuntime(replayContext, authority))
        {
            await replayRuntime.ReplayService.ReplayAsync(CancellationToken.None);
        }

        int checkpointCount;
        int outboxCount;
        await using (var committedContext = fixture.CreateDbContext())
        {
            Location[] homes = await committedContext.Locations
                .IgnoreQueryFilters()
                .Include(location => location.Pii)
                .Where(location => graph.LocationIds.Contains(location.Id))
                .OrderBy(location => location.Id)
                .ToArrayAsync();
            LocationRoom[] rooms = await committedContext.LocationRooms
                .IgnoreQueryFilters()
                .Where(room => graph.RoomIds.Contains(room.Id))
                .ToArrayAsync();
            EventLocation[] eventLocations = await committedContext.EventLocations
                .IgnoreQueryFilters()
                .Where(eventLocation => graph.EventLocationIds.Contains(eventLocation.Id))
                .OrderBy(eventLocation => eventLocation.Id)
                .ToArrayAsync();
            OutboxMessage[] messages = await committedContext.OutboxMessages
                .Where(message =>
                    message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
                    || message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType)
                .OrderBy(message => message.Id)
                .ToArrayAsync();
            PrivacyErasureReplayCheckpoint checkpoint = await committedContext
                .PrivacyErasureReplayCheckpoints
                .SingleAsync();
            PrivacyErasureIntent localMirror = await committedContext
                .PrivacyErasureIntents
                .SingleAsync();
            ActorPii ownerActorPii = await committedContext.ActorPii
                .IgnoreQueryFilters()
                .SingleAsync(pii => pii.ActorId == graph.OwnerActorId);

            await Assert.That(homes.All(home =>
                home.LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Erased
                && home.OwnerUserId is null
                && home.Pii is null
                && home.FullName == Location.ErasedPrivateVenueLabel
                && home.City == string.Empty)).IsTrue();
            await Assert.That(rooms.All(room =>
                room.IsDeleted
                && room.Name.StartsWith("privacy-erased-", StringComparison.Ordinal)
                && room.Description is null
                && graph.LocationIds.Contains(room.LocationId))).IsTrue();
            await Assert.That(eventLocations.All(eventLocation =>
                eventLocation.LocationId.HasValue
                && graph.LocationIds.Contains(eventLocation.LocationId.Value)
                && eventLocation.NeedsPrivacyReview
                && eventLocation.FullDetailsAudienceId == (int)LocationDisclosureAudienceEnum.Never
                && eventLocation.PolicyVersion == 2)).IsTrue();
            await Assert.That(await committedContext.Users
                .IgnoreQueryFilters()
                .AnyAsync(user => user.Id == graph.OwnerUserId && user.IsDeleted)).IsTrue();
            await Assert.That(await committedContext.UserPii
                .AnyAsync(pii => pii.UserId == graph.OwnerUserId)).IsFalse();
            await Assert.That(ownerActorPii.DisplayName).StartsWith("DeletedUser");
            await Assert.That(ownerActorPii.Did).IsNull();
            await Assert.That(ownerActorPii.Handle).IsNull();
            await Assert.That(ownerActorPii.ProfilePictureUri).IsNull();
            await Assert.That(checkpoint.AuthoritySequence).IsEqualTo(retained.AuthoritySequence);
            await Assert.That(checkpoint.IntentId).IsEqualTo(retained.IntentId);
            await Assert.That(localMirror.IntentId).IsEqualTo(retained.IntentId);
            await Assert.That(localMirror.AuthoritySequence).IsEqualTo(retained.AuthoritySequence);
            await Assert.That(localMirror.SubjectId).IsEqualTo(retained.SubjectId);
            await Assert.That(localMirror.SubjectKind).IsEqualTo(retained.SubjectKind);
            await Assert.That(messages.Count(message =>
                message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType))
                .IsEqualTo(2);
            await Assert.That(messages.Count(message =>
                message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType))
                .IsEqualTo(2);
            await Assert.That(messages.All(message => message.Id.Version == 7)).IsTrue();
            string payloads = string.Join('|', messages.Select(message => message.Payload));
            foreach (string piiCanary in graph.PiiCanaries)
            {
                await Assert.That(payloads).DoesNotContain(piiCanary);
            }

            checkpointCount = await committedContext.PrivacyErasureReplayCheckpoints.CountAsync();
            outboxCount = messages.Length;
        }

        await using (var restartedContext = fixture.CreateDbContext())
        await using (ErasureRuntime restartedRuntime = CreateRuntime(restartedContext, authority))
        {
            await restartedRuntime.ReplayService.ReplayAsync(CancellationToken.None);
        }

        await using (var finalContext = fixture.CreateDbContext())
        {
            await Assert.That(await finalContext.PrivacyErasureReplayCheckpoints.CountAsync())
                .IsEqualTo(checkpointCount);
            await Assert.That(await finalContext.OutboxMessages.CountAsync(message =>
                message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
                || message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType))
                .IsEqualTo(outboxCount);
        }

        ErasureGraph restoredGraph;
        long checkpointBeforeRestore;
        await using (var seedContext = fixture.CreateDbContext())
        {
            restoredGraph = await SeedErasureGraphAsync(seedContext);
            checkpointBeforeRestore = await seedContext.PrivacyErasureReplayCheckpoints
                .MaxAsync(checkpoint => (long?)checkpoint.AuthoritySequence)
                ?? 0;
        }

        PrivacyErasureIntent retainedAfterRestore = await authority.AppendAsync(
            new PrivacyErasureRequest(
                Guid.CreateVersion7(),
                PrivacyErasureSubjectKind.User,
                restoredGraph.OwnerUserId,
                PrivacyErasureReasonCode.AccountDeletion,
                1));
        await Assert.That(retainedAfterRestore.AuthoritySequence).IsEqualTo(checkpointBeforeRestore + 1);

        await using (var restoredContext = fixture.CreateDbContext())
        await using (ErasureRuntime restored = CreateRuntime(restoredContext, authority))
        {
            await restored.ReplayService.ReplayAsync(CancellationToken.None);
        }

        int checkpointCountAfterRestore;
        int outboxCountAfterRestore;
        await using (var verifiedContext = fixture.CreateDbContext())
        {
            Location[] homes = await verifiedContext.Locations
                .IgnoreQueryFilters()
                .Include(location => location.Pii)
                .Where(location => restoredGraph.LocationIds.Contains(location.Id))
                .ToArrayAsync();
            checkpointCountAfterRestore = await verifiedContext.PrivacyErasureReplayCheckpoints.CountAsync();
            outboxCountAfterRestore = await verifiedContext.OutboxMessages.CountAsync(message =>
                message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
                || message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType);

            await Assert.That(homes.All(home =>
                home.LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Erased
                && home.OwnerUserId is null
                && home.Pii == null)).IsTrue();
            await Assert.That(await verifiedContext.PrivacyErasureReplayCheckpoints
                .AnyAsync(checkpoint => checkpoint.AuthoritySequence == retainedAfterRestore.AuthoritySequence
                    && checkpoint.IntentId == retainedAfterRestore.IntentId)).IsTrue();
        }

        await using (var restartedContext = fixture.CreateDbContext())
        await using (ErasureRuntime restarted = CreateRuntime(restartedContext, authority))
        {
            await restarted.ReplayService.ReplayAsync(CancellationToken.None);
        }

        await using var postRestoreContext = fixture.CreateDbContext();
        await Assert.That(await postRestoreContext.PrivacyErasureReplayCheckpoints.CountAsync())
            .IsEqualTo(checkpointCountAfterRestore);
        await Assert.That(await postRestoreContext.OutboxMessages.CountAsync(message =>
            message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
            || message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType))
            .IsEqualTo(outboxCountAfterRestore);
    }

    private static ErasureRuntime CreateRuntime(
        ExploreDbContext context,
        IPrivacyErasureAuthority authority)
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        ServiceProvider cacheProvider = services.BuildServiceProvider();
        var userRepository = new UserRepository(context);
        var userPiiRepository = new GenericRepository<UserPii, Guid>(context);
        var tokenRepository = new UserAuthenticationTokenRepository(context);
        var erasureRepository = new UserLocationPrivacyErasureRepository(context);
        var checkpointRepository = new PrivacyErasureReplayCheckpointRepository(context);
        var outboxRepository = new OutboxRepository(context);
        IPrivacyErasureLedgerRepository ledgerRepository =
            new ApplicationDatabasePrivacyErasureLedgerRepository(context, TimeProvider.System);
        HybridCache cache = cacheProvider.GetRequiredService<HybridCache>();
        var applier = new PrivacyErasureApplier(
            userRepository,
            userPiiRepository,
            tokenRepository,
            erasureRepository,
            checkpointRepository,
            ledgerRepository,
            outboxRepository,
            cache,
            TimeProvider.System,
            NullLogger<PrivacyErasureApplier>.Instance);
        var service = new RetainedAuthorityPrivacyErasureWorkflow(
            userRepository,
            checkpointRepository,
            authority,
            new EfCoreUnitOfWork(context),
            applier);
        return new ErasureRuntime(
            service,
            new PrivacyErasureReplayService(service),
            cacheProvider);
    }

    internal static async Task<ErasureGraph> SeedErasureGraphAsync(ExploreDbContext context)
    {
        var tenantA = CreateTenant("workflow-a");
        var tenantB = CreateTenant("workflow-b");
        var owner = CreateUser("workflow-owner");
        context.AddRange(tenantA, tenantB, owner);
        await context.SaveChangesAsync();

        var ownerActor = new Actor
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantA.Id,
            Tenant = null!,
            UserId = owner.Id,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            Pii = new ActorPii
            {
                DisplayName = "ACTOR-NAME-CANARY",
                Did = "did:plc:actor-canary",
                Handle = "actor-canary.example",
                ProfilePictureUri = "https://example.com/actor-canary.jpg",
            },
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        var tenantBActor = new Actor
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantB.Id,
            Tenant = null!,
            ActorTypeId = (int)ActorTypeEnum.Group,
            ActorType = null!,
            Pii = new ActorPii { DisplayName = "Tenant B organizer" },
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.Actors.AddRange(ownerActor, tenantBActor);
        await context.SaveChangesAsync();

        Location homeA = CreatePrivateHome(tenantA.Id, owner.Id, "HOME-A-NAME-CANARY");
        Location homeB = CreatePrivateHome(tenantB.Id, owner.Id, "HOME-B-NAME-CANARY");
        homeA.Pii!.Address = "HOME-A-ADDRESS-CANARY";
        homeB.Pii!.Address = "HOME-B-ADDRESS-CANARY";
        context.Locations.AddRange(homeA, homeB);
        await context.SaveChangesAsync();

        var roomA = new LocationRoom
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantA.Id,
            Tenant = null!,
            LocationId = homeA.Id,
            Location = null!,
            Name = "HOME-A-ROOM-CANARY",
            Description = "HOME-A-ROOM-DESCRIPTION-CANARY",
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        var roomB = new LocationRoom
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantB.Id,
            Tenant = null!,
            LocationId = homeB.Id,
            Location = null!,
            Name = "HOME-B-ROOM-CANARY",
            Description = "HOME-B-ROOM-DESCRIPTION-CANARY",
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.LocationRooms.AddRange(roomA, roomB);

        Explore.Domain.Event eventA = CreateEvent(tenantA.Id, ownerActor.Id, "Home A event");
        Explore.Domain.Event eventB = CreateEvent(tenantB.Id, tenantBActor.Id, "Home B event");
        context.Events.AddRange(eventA, eventB);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE location_rooms SET is_deleted = TRUE, deleted_at = NOW() WHERE id = {roomB.Id}");

        EventLocation eventLocationA = EventLocation.CreatePhysical(
            tenantA.Id, eventA.Id, homeA.Id, owner.Id, DateTime.UtcNow);
        EventLocation eventLocationB = EventLocation.CreatePhysical(
            tenantB.Id, eventB.Id, homeB.Id, owner.Id, DateTime.UtcNow);
        var eventLocationRepository = new EventLocationRepository(context);
        await eventLocationRepository.AddAsync(eventLocationA, CancellationToken.None);
        await eventLocationRepository.AddAsync(eventLocationB, CancellationToken.None);

        return new ErasureGraph(
            owner.Id,
            ownerActor.Id,
            [homeA.Id, homeB.Id],
            [roomA.Id, roomB.Id],
            [eventLocationA.Id, eventLocationB.Id],
            [homeA.FullName, homeB.FullName],
            [
                owner.Pii.Email,
                owner.Pii.FirstName,
                owner.Pii.LastName,
                ownerActor.Pii!.DisplayName,
                homeA.FullName,
                homeB.FullName,
                homeA.Pii!.Address,
                homeB.Pii!.Address,
                roomA.Name,
                roomB.Name,
                roomA.Description!,
                roomB.Description!,
            ]);
    }

    private static Explore.Domain.Event CreateEvent(Guid tenantId, Guid actorId, string title) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        Tenant = null!,
        ActorId = actorId,
        Actor = null!,
        Title = title,
        EventStatusId = (int)EventStatusEnum.Draft,
        EventStatus = null!,
        EventFormatId = (int)EventFormatEnum.Local,
        EventFormat = null!,
        VisibilityTypeId = (int)VisibilityTypeEnum.Private,
        VisibilityType = null!,
        ConcurrencyStamp = Guid.CreateVersion7(),
    };

    internal static Task InstallOutboxFailureTriggerAsync(ExploreDbContext context) =>
        context.Database.ExecuteSqlRawAsync(
            """
            CREATE OR REPLACE FUNCTION reject_location_privacy_outbox() RETURNS trigger
            LANGUAGE plpgsql AS $function$
            BEGIN
                IF NEW.event_type IN ('LocationPiiErased', 'LocationPrivacyCorrectionRequested') THEN
                    RAISE EXCEPTION 'forced location privacy outbox rollback';
                END IF;
                RETURN NEW;
            END;
            $function$;
            DROP TRIGGER IF EXISTS tr_reject_location_privacy_outbox ON outbox_messages;
            CREATE TRIGGER tr_reject_location_privacy_outbox
                BEFORE INSERT ON outbox_messages
                FOR EACH ROW EXECUTE FUNCTION reject_location_privacy_outbox();
            """);

    internal static Task RemoveOutboxFailureTriggerAsync(ExploreDbContext context) =>
        context.Database.ExecuteSqlRawAsync(
            """
            DROP TRIGGER IF EXISTS tr_reject_location_privacy_outbox ON outbox_messages;
            DROP FUNCTION IF EXISTS reject_location_privacy_outbox();
            """);

    private static Tenant CreateTenant(string slug)
    {
        return new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = slug,
            Slug = $"{slug}-{Guid.NewGuid():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
    }

    private static User CreateUser(string emailPrefix)
    {
        return new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com",
                FirstName = "Privacy",
                LastName = "Owner",
            },
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static Location CreatePrivateHome(Guid tenantId, Guid ownerUserId, string name)
    {
        var location = CreateLocation(tenantId, name);
        location.ClassifyAsPrivateHome(ownerUserId);
        location.AttachPii(new LocationPii
        {
            LocationId = location.Id,
            Address = $"{name} address",
            Postcode = "1000",
        });
        return location;
    }

    private static Location CreateNonHome(Guid tenantId, string name)
    {
        var location = CreateLocation(tenantId, name);
        location.ClassifyAs(LocationKindEnum.CommercialVenue);
        return location;
    }

    private static Location CreateLocation(Guid tenantId, string name)
    {
        return new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            FullName = name,
            Country = "BE",
            City = "Brussels",
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
    }

    private sealed record ErasureRuntime(
        RetainedAuthorityPrivacyErasureWorkflow Service,
        PrivacyErasureReplayService ReplayService,
        ServiceProvider CacheProvider) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => CacheProvider.DisposeAsync();
    }

    internal sealed record ErasureGraph(
        Guid OwnerUserId,
        Guid OwnerActorId,
        Guid[] LocationIds,
        Guid[] RoomIds,
        Guid[] EventLocationIds,
        string[] HomeNames,
        string[] PiiCanaries);

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}

[Category("EventLocationPrivacy")]
[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class CoLocatedPrivacyErasureAuthorityTests(
    PostgreSqlContainerFixture fixture)
{
    [Test]
    [Timeout(240_000)]
    public async Task AuthorityAppend_SurvivesApplicationRollback_AndReplayConvergesExactlyOnce()
    {
        GlobalLocationPrivacyErasureTests.ErasureGraph graph;
        int ledgerBefore;
        int counterBefore;
        int checkpointBefore;
        int outboxBefore;
        await using (var seedContext = fixture.CreateDbContext())
        {
            graph = await GlobalLocationPrivacyErasureTests.SeedErasureGraphAsync(seedContext);
            ledgerBefore = await seedContext.PrivacyErasureIntents.CountAsync();
            counterBefore = await seedContext.PrivacyErasureCounters.CountAsync();
            checkpointBefore = await seedContext.PrivacyErasureReplayCheckpoints.CountAsync();
            outboxBefore = await CountPrivacyOutboxAsync(seedContext);
            await GlobalLocationPrivacyErasureTests.InstallOutboxFailureTriggerAsync(seedContext);
        }

        try
        {
            await using var failingContext = fixture.CreateDbContext();
            await using CoLocatedErasureRuntime failingRuntime = CreateRuntime(failingContext);
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                failingRuntime.Service.EraseUserAsync(graph.OwnerUserId, CancellationToken.None));
        }
        finally
        {
            await using var triggerContext = fixture.CreateDbContext();
            await GlobalLocationPrivacyErasureTests.RemoveOutboxFailureTriggerAsync(triggerContext);
        }

        await using (var rollbackContext = fixture.CreateDbContext())
        {
            await Assert.That(await rollbackContext.PrivacyErasureIntents.CountAsync())
                .IsEqualTo(ledgerBefore + 1);
            await Assert.That(await rollbackContext.PrivacyErasureCounters.CountAsync())
                .IsEqualTo(Math.Max(counterBefore, 1));
            await Assert.That(await rollbackContext.PrivacyErasureReplayCheckpoints.CountAsync())
                .IsEqualTo(checkpointBefore);
            await Assert.That(await CountPrivacyOutboxAsync(rollbackContext)).IsEqualTo(outboxBefore);
            await Assert.That(await rollbackContext.PrivacyErasureIntents
                .AnyAsync(intent => intent.SubjectId == graph.OwnerUserId)).IsTrue();
            await AssertGraphIsActiveAsync(rollbackContext, graph);
        }

        await using (var replayContext = fixture.CreateDbContext())
        await using (CoLocatedErasureRuntime replayRuntime = CreateRuntime(replayContext))
        {
            await replayRuntime.ReplayService.ReplayAsync(CancellationToken.None);
        }

        long factSequence;
        int checkpointAfterReplay;
        int outboxAfterReplay;
        await using (var verifyContext = fixture.CreateDbContext())
        {
            PrivacyErasureIntent fact = await verifyContext.PrivacyErasureIntents
                .SingleAsync(intent => intent.SubjectId == graph.OwnerUserId);
            PrivacyErasureReplayCheckpoint checkpoint = await verifyContext
                .PrivacyErasureReplayCheckpoints
                .SingleAsync(item => item.IntentId == fact.IntentId);
            PrivacyErasureCounter counter = await verifyContext
                .PrivacyErasureCounters
                .SingleAsync();
            checkpointAfterReplay = await verifyContext.PrivacyErasureReplayCheckpoints.CountAsync();
            outboxAfterReplay = await CountPrivacyOutboxAsync(verifyContext);
            factSequence = fact.AuthoritySequence;

            await Assert.That(checkpointAfterReplay).IsEqualTo(checkpointBefore + 1);
            await Assert.That(outboxAfterReplay).IsEqualTo(outboxBefore + 4);
            await Assert.That(checkpoint.AuthoritySequence).IsEqualTo(factSequence);
            await Assert.That(counter.LastSequence).IsEqualTo(factSequence);
            await AssertGraphIsErasedAsync(verifyContext, graph);
        }

        await using (var repeatedContext = fixture.CreateDbContext())
        await using (CoLocatedErasureRuntime repeatedRuntime = CreateRuntime(repeatedContext))
        {
            await repeatedRuntime.ReplayService.ReplayAsync(CancellationToken.None);
        }

        await using var finalContext = fixture.CreateDbContext();
        await Assert.That(await finalContext.PrivacyErasureReplayCheckpoints.CountAsync())
            .IsEqualTo(checkpointAfterReplay);
        await Assert.That(await CountPrivacyOutboxAsync(finalContext)).IsEqualTo(outboxAfterReplay);
        await Assert.That(await finalContext.PrivacyErasureIntents
            .CountAsync(intent => intent.AuthoritySequence == factSequence)).IsEqualTo(1);
    }

    [Test]
    [Timeout(240_000)]
    public async Task Authority_DuplicateMismatchCancellationAndConcurrentAppendsPreserveContiguousSequence()
    {
        var authority = new CoLocatedPrivacyErasureAuthorityRepository(
            new FixtureExploreDbContextFactory(fixture),
            TimeProvider.System);
        var firstRequest = CreateIntentRequest();
        PrivacyErasureIntent first = await authority.AppendAsync(firstRequest);
        PrivacyErasureIntent duplicate = await authority.AppendAsync(firstRequest);
        await Assert.That(duplicate.AuthoritySequence).IsEqualTo(first.AuthoritySequence);

        var mismatch = new PrivacyErasureRequest(
            firstRequest.IntentId,
            PrivacyErasureSubjectKind.User,
            Guid.CreateVersion7(),
            firstRequest.ReasonCode,
            firstRequest.PolicyVersion);
        await Assert.ThrowsAsync<InvalidOperationException>(() => authority.AppendAsync(mismatch));

        var cancelledRequest = CreateIntentRequest();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            authority.AppendAsync(cancelledRequest, new CancellationToken(canceled: true)));

        PrivacyErasureRequest[] concurrentRequests = Enumerable.Range(0, 8)
            .Select(_ => CreateIntentRequest())
            .ToArray();
        PrivacyErasureIntent[] concurrent = await Task.WhenAll(
            concurrentRequests.Select(request => authority.AppendAsync(request)));
        await Assert.That(concurrent.Select(item => item.AuthoritySequence))
            .IsEquivalentTo(Enumerable.Range(1, concurrentRequests.Length)
                .Select(offset => first.AuthoritySequence + offset));

        await using var verifyContext = fixture.CreateDbContext();
        long[] sequences = await verifyContext.PrivacyErasureIntents
            .OrderBy(item => item.AuthoritySequence)
            .Select(item => item.AuthoritySequence)
            .ToArrayAsync();
        await Assert.That(sequences).IsEquivalentTo(
            Enumerable.Range(1, sequences.Length).Select(value => (long)value));
        await Assert.That(await verifyContext.PrivacyErasureCounters
            .Select(counter => counter.LastSequence)
            .SingleAsync()).IsEqualTo(sequences[^1]);
        await Assert.That(await verifyContext.PrivacyErasureIntents
            .AnyAsync(intent => intent.IntentId == cancelledRequest.IntentId)).IsFalse();
    }

    private CoLocatedErasureRuntime CreateRuntime(ExploreDbContext context)
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        ServiceProvider cacheProvider = services.BuildServiceProvider();
        var userRepository = new UserRepository(context);
        var erasureRepository = new UserLocationPrivacyErasureRepository(context);
        IPrivacyErasureLedgerRepository ledgerRepository =
            new ApplicationDatabasePrivacyErasureLedgerRepository(context, TimeProvider.System);
        var applier = new PrivacyErasureApplier(
            userRepository,
            new GenericRepository<UserPii, Guid>(context),
            new UserAuthenticationTokenRepository(context),
            erasureRepository,
            new PrivacyErasureReplayCheckpointRepository(context),
            ledgerRepository,
            new OutboxRepository(context),
            cacheProvider.GetRequiredService<HybridCache>(),
            TimeProvider.System,
            NullLogger<PrivacyErasureApplier>.Instance);
        var service = new RetainedAuthorityPrivacyErasureWorkflow(
                userRepository,
                new PrivacyErasureReplayCheckpointRepository(context),
                new CoLocatedPrivacyErasureAuthorityRepository(
                    new FixtureExploreDbContextFactory(fixture),
                    TimeProvider.System),
                new EfCoreUnitOfWork(context),
                applier);
        return new CoLocatedErasureRuntime(
            service,
            new PrivacyErasureReplayService(service),
            cacheProvider);
    }

    private static PrivacyErasureRequest CreateIntentRequest() => new(
        Guid.CreateVersion7(),
        PrivacyErasureSubjectKind.User,
        Guid.CreateVersion7(),
        PrivacyErasureReasonCode.AccountDeletion,
        1);

    private static Task<int> CountPrivacyOutboxAsync(ExploreDbContext context) =>
        context.OutboxMessages.CountAsync(message =>
            message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
            || message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType);

    private static async Task AssertGraphIsActiveAsync(
        ExploreDbContext context,
        GlobalLocationPrivacyErasureTests.ErasureGraph graph)
    {
        Location[] homes = await context.Locations
            .IgnoreQueryFilters()
            .Include(location => location.Pii)
            .Where(location => graph.LocationIds.Contains(location.Id))
            .ToArrayAsync();
        await Assert.That(homes.All(home =>
            home.LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Active
            && home.OwnerUserId == graph.OwnerUserId
            && home.Pii is not null)).IsTrue();
        await Assert.That(await context.Users.IgnoreQueryFilters()
            .AnyAsync(user => user.Id == graph.OwnerUserId && !user.IsDeleted)).IsTrue();
        await Assert.That(await context.UserPii.AnyAsync(pii => pii.UserId == graph.OwnerUserId)).IsTrue();
    }

    private static async Task AssertGraphIsErasedAsync(
        ExploreDbContext context,
        GlobalLocationPrivacyErasureTests.ErasureGraph graph)
    {
        Location[] homes = await context.Locations
            .IgnoreQueryFilters()
            .Include(location => location.Pii)
            .Where(location => graph.LocationIds.Contains(location.Id))
            .ToArrayAsync();
        await Assert.That(homes.All(home =>
            home.LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Erased
            && home.OwnerUserId is null
            && home.Pii is null)).IsTrue();
        await Assert.That(await context.Users.IgnoreQueryFilters()
            .AnyAsync(user => user.Id == graph.OwnerUserId && user.IsDeleted)).IsTrue();
        await Assert.That(await context.UserPii.AnyAsync(pii => pii.UserId == graph.OwnerUserId)).IsFalse();
    }

    private sealed record CoLocatedErasureRuntime(
        RetainedAuthorityPrivacyErasureWorkflow Service,
        PrivacyErasureReplayService ReplayService,
        ServiceProvider CacheProvider) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => CacheProvider.DisposeAsync();
    }

    private sealed class FixtureExploreDbContextFactory(
        PostgreSqlContainerFixture fixture) : IDbContextFactory<ExploreDbContext>
    {
        public ExploreDbContext CreateDbContext() => fixture.CreateDbContext();
    }
}

[Category("EventLocationPrivacy")]
[ClassDataSource<GlobalLocationPrivacyPostgreSqlFixture>(Shared = SharedType.PerClass)]
[NotInParallel("PersistenceDb")]
public sealed class EfCoreRetainedPrivacyErasureAuthorityTests(
    GlobalLocationPrivacyPostgreSqlFixture fixture)
{
    [Test]
    [Timeout(240_000)]
    public async Task RuntimeRole_AppendsAndReadsThroughFunctionsButCannotReadAuthorityTables()
    {
        await using PrivacyErasureAuthorityDbContext context = fixture.CreateAuthorityDbContext();
        var repository = new EfCorePrivacyErasureAuthorityRepository(context);
        var request = new PrivacyErasureRequest(
            Guid.CreateVersion7(),
            PrivacyErasureSubjectKind.User,
            Guid.CreateVersion7(),
            PrivacyErasureReasonCode.AccountDeletion,
            1);

        PrivacyErasureIntent first = await repository.AppendAsync(request);
        PrivacyErasureIntent duplicate = await repository.AppendAsync(request);
        IReadOnlyList<PrivacyErasureIntent> facts =
            await repository.ReadAfterAsync(0, 10);

        await Assert.That(duplicate.AuthoritySequence).IsEqualTo(first.AuthoritySequence);
        await Assert.That(facts.Count).IsEqualTo(1);
        await Assert.That(facts.Single().IntentId).IsEqualTo(first.IntentId);

        await context.Database.OpenConnectionAsync();
        try
        {
            var connection = (NpgsqlConnection)context.Database.GetDbConnection();
            await using var command = new NpgsqlCommand(
                "SELECT count(*) FROM location_privacy_authority.erasure_intents",
                connection);
            PostgresException? exception = await Assert.ThrowsAsync<PostgresException>(() =>
                command.ExecuteScalarAsync());
            await Assert.That(exception!.SqlState).IsEqualTo(PostgresErrorCodes.InsufficientPrivilege);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}

public sealed class GlobalLocationPrivacyPostgreSqlFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string AuthorityRuntimeUsername = "global_erasure_runtime";
    private const string AuthorityRuntimePassword = "global-erasure-runtime-password";
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("global_location_privacy_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    private readonly PostgreSqlContainer _authorityContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("global_location_privacy_authority_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    private string _authorityRuntimeConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_container.StartAsync(), _authorityContainer.StartAsync());
        await using (PrivacyErasureAuthorityDbContext authorityContext = CreateAuthorityAdminDbContext())
        {
            await authorityContext.Database.MigrateAsync();
        }
        await using (var connection = new NpgsqlConnection(_authorityContainer.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var roleCommand = connection.CreateCommand();
            roleCommand.CommandText =
                $"""
                CREATE ROLE {AuthorityRuntimeUsername} LOGIN PASSWORD '{AuthorityRuntimePassword}';
                GRANT location_privacy_authority_runtime TO {AuthorityRuntimeUsername};
                """;
            await roleCommand.ExecuteNonQueryAsync();
        }
        _authorityRuntimeConnectionString = new NpgsqlConnectionStringBuilder(
            _authorityContainer.GetConnectionString())
        {
            Username = AuthorityRuntimeUsername,
            Password = AuthorityRuntimePassword,
        }.ConnectionString;

        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        context.Set<TenantStatus>().Add(new TenantStatus
        {
            Id = (int)TenantStatusEnum.Active,
            MasterCode = "ACTIVE",
            FullName = "Active",
            IsActiveState = true,
        });
        context.Set<ActorType>().AddRange(
            new ActorType
            {
                Id = (int)ActorTypeEnum.User,
                MasterCode = "USER",
                FullName = "User",
            },
            new ActorType
            {
                Id = (int)ActorTypeEnum.Group,
                MasterCode = "GROUP",
                FullName = "Group",
            });
        context.Set<EventStatus>().Add(new EventStatus
        {
            Id = (int)EventStatusEnum.Draft,
            MasterCode = "DRAFT",
            FullName = "Draft",
        });
        context.Set<EventFormat>().Add(new EventFormat
        {
            Id = (int)EventFormatEnum.Local,
            MasterCode = "LOCAL",
            FullName = "Local",
        });
        context.Set<VisibilityType>().Add(new VisibilityType
        {
            Id = (int)VisibilityTypeEnum.Private,
            MasterCode = "PRIVATE",
            FullName = "Private",
        });
        await context.SaveChangesAsync();
        await LookupTableSeeder.SeedLocationPrivacyLookupsAsync(context, CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
        await _authorityContainer.StopAsync();
        await _authorityContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public PrivacyErasureAuthorityDbContext CreateAuthorityDbContext()
    {
        var options = new DbContextOptionsBuilder<PrivacyErasureAuthorityDbContext>()
            .UseNpgsql(_authorityRuntimeConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PrivacyErasureAuthorityDbContext(options);
    }

    private PrivacyErasureAuthorityDbContext CreateAuthorityAdminDbContext()
    {
        var options = new DbContextOptionsBuilder<PrivacyErasureAuthorityDbContext>()
            .UseNpgsql(_authorityContainer.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PrivacyErasureAuthorityDbContext(options);
    }

    public ExploreDbContext CreateDbContext()
        => CreateDbContext(enableRetryOnFailure: false);

    public ExploreDbContext CreateRetryingDbContext()
        => CreateDbContext(enableRetryOnFailure: true);

    private ExploreDbContext CreateDbContext(bool enableRetryOnFailure)
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
            {
                if (enableRetryOnFailure)
                {
                    npgsql.EnableRetryOnFailure();
                }
            })
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Global location privacy test seed context.");
        return context;
    }

    public ExploreDbContext CreateTenantFilteredDbContext(ITenantContext? tenantContext = null)
    {
        var context = CreateDbContext();
        context.ClearTenantFilterBypass();
        context.TenantContext = tenantContext;
        return context;
    }

}
