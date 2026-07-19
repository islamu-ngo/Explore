// ABOUTME: PostgreSQL proofs for the owner-bounded cross-tenant Private Home erasure query.
// ABOUTME: Prevents global account deletion from enumerating unrelated owners or non-Home locations.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Infrastructure.Privacy.ErasureAuthority;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
                    .IgnoreTenantFilter(TenantFilterBypassReasons.GlobalLocationPrivacyErasure)
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
            .IgnoreTenantFilter(TenantFilterBypassReasons.GlobalLocationPrivacyErasure)
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
    public async Task AuthorityFirstRollback_LeavesPendingFactThenSequenceZeroReplayCommitsTombstonesCheckpointAndPiiFreeOutbox()
    {
        ErasureGraph graph;
        await using (var seedContext = fixture.CreateDbContext())
        {
            graph = await SeedErasureGraphAsync(seedContext);
            await InstallOutboxFailureTriggerAsync(seedContext);
        }

        await using PostgreSqlLocationPrivacyErasureAuthority authority =
            fixture.CreateAuthorityClient();
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

        LocationPrivacyErasureAuthorityIntent retained;
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
            await Assert.That(await rollbackContext.LocationPrivacyErasureReplayCheckpoints.CountAsync())
                .IsEqualTo(0);
            await Assert.That(await rollbackContext.OutboxMessages.CountAsync(message =>
                message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
                || message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType))
                .IsEqualTo(0);
        }

        IReadOnlyList<LocationPrivacyErasureAuthorityIntent> pending =
            await authority.ReadAfterAsync(0, 10);
        await Assert.That(pending.Count).IsEqualTo(1);
        retained = pending.Single();
        await Assert.That(retained.OwnerUserId).IsEqualTo(graph.OwnerUserId);
        await Assert.That(retained.LocationIds).IsEquivalentTo(graph.LocationIds);

        LocationPrivacyErasureAuthorityIntent duplicate = await authority.AppendAsync(
            new LocationPrivacyErasureIntent(
                retained.IntentId,
                retained.OwnerUserId,
                retained.LocationIds,
                retained.Reason));
        await Assert.That(duplicate.AuthoritySequence).IsEqualTo(retained.AuthoritySequence);
        await Assert.That((await authority.ReadAfterAsync(0, 10)).Count).IsEqualTo(1);

        await using (var replayContext = fixture.CreateDbContext())
        await using (ErasureRuntime replayRuntime = CreateRuntime(replayContext, authority))
        {
            await replayRuntime.Service.ReplayPendingAsync(CancellationToken.None);
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
            LocationPrivacyErasureReplayCheckpoint checkpoint = await committedContext
                .LocationPrivacyErasureReplayCheckpoints
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

            checkpointCount = await committedContext.LocationPrivacyErasureReplayCheckpoints.CountAsync();
            outboxCount = messages.Length;
        }

        await using (var restartedContext = fixture.CreateDbContext())
        await using (ErasureRuntime restartedRuntime = CreateRuntime(restartedContext, authority))
        {
            await restartedRuntime.Service.ReplayPendingAsync(CancellationToken.None);
        }

        await using var finalContext = fixture.CreateDbContext();
        await Assert.That(await finalContext.LocationPrivacyErasureReplayCheckpoints.CountAsync())
            .IsEqualTo(checkpointCount);
        await Assert.That(await finalContext.OutboxMessages.CountAsync(message =>
            message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
            || message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType))
            .IsEqualTo(outboxCount);
    }

    private static ErasureRuntime CreateRuntime(
        ExploreDbContext context,
        ILocationPrivacyErasureAuthority authority)
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        ServiceProvider cacheProvider = services.BuildServiceProvider();
        var service = new GlobalLocationPrivacyErasureService(
            new UserRepository(context),
            new GenericRepository<UserPii, Guid>(context),
            new UserAuthenticationTokenRepository(context),
            new GlobalLocationPrivacyErasureRepository(context),
            new LocationPrivacyErasureReplayCheckpointRepository(context),
            new OutboxRepository(context),
            authority,
            new EfCoreUnitOfWork(context),
            cacheProvider.GetRequiredService<HybridCache>(),
            TimeProvider.System,
            NullLogger<GlobalLocationPrivacyErasureService>.Instance);
        return new ErasureRuntime(service, cacheProvider);
    }

    private static async Task<ErasureGraph> SeedErasureGraphAsync(ExploreDbContext context)
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

    private static Task InstallOutboxFailureTriggerAsync(ExploreDbContext context) =>
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

    private static Task RemoveOutboxFailureTriggerAsync(ExploreDbContext context) =>
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
        GlobalLocationPrivacyErasureService Service,
        ServiceProvider CacheProvider) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => CacheProvider.DisposeAsync();
    }

    private sealed record ErasureGraph(
        Guid OwnerUserId,
        Guid OwnerActorId,
        Guid[] LocationIds,
        Guid[] RoomIds,
        Guid[] EventLocationIds,
        string[] HomeNames,
        string[] PiiCanaries);

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}

public sealed class GlobalLocationPrivacyPostgreSqlFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string ApplicationMigration = "20260718205141_ProtectAtprotoOAuthSessions";
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
        await using (var connection = new NpgsqlConnection(_authorityContainer.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var schemaCommand = connection.CreateCommand();
            schemaCommand.CommandText = LocationPrivacyErasureAuthoritySchema.ReadProvisioningSql();
            await schemaCommand.ExecuteNonQueryAsync();
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
        await context.Database.MigrateAsync(ApplicationMigration);
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
    }

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
        await _authorityContainer.StopAsync();
        await _authorityContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public PostgreSqlLocationPrivacyErasureAuthority CreateAuthorityClient() =>
        new(Options.Create(new LocationPrivacyErasureAuthorityOptions
        {
            ConnectionString = _authorityRuntimeConnectionString,
        }));

    public ExploreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(_container.GetConnectionString())
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
