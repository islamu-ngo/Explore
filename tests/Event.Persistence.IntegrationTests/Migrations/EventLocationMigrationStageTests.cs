// ABOUTME: PostgreSQL acceptance tests for the Event Location Privacy Expand migration and stage gate.
// ABOUTME: Verifies fresh/legacy rollout, rollback, immutable privacy evidence, and carrier consistency.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using Explore.Persistence.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
[Property("Category", "EventLocationPrivacy")]
public sealed class EventLocationMigrationStageTests(RecipientDeliveryMigrationContainerFixture fixture)
{
    private const string PreviousMigration = "20260715172404_AddTypedWebhookOwnership";
    private const string BackfillPreviousMigration = "20260718210538_HardenAtprotoFederationPersistence";
    private const string BackfillMigration = "20260718215537_BackfillUnclassifiedEventLocations";

    [Test]
    public async Task GenericMigrator_RequiresExplicitPendingStage_AndRejectsUnavailableContract()
    {
        await WithDatabaseAsync(async context =>
        {
            var configuration = new ConfigurationManager();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ExploreDatabaseMigrator.MigrateAsync(context, configuration));

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = " ";
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ExploreDatabaseMigrator.MigrateAsync(context, configuration));

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = "Backfill";
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ExploreDatabaseMigrator.MigrateAsync(context, configuration));

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = "Contract";
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ExploreDatabaseMigrator.MigrateAsync(context, configuration));

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = "Expand";
            await ExploreDatabaseMigrator.MigrateAsync(context, configuration);

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = null;
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ExploreDatabaseMigrator.MigrateAsync(context, configuration));

            string[] applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            await Assert.That(applied).Contains(EventLocationPrivacyMigrationStage.ExpandMigration);

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = "Contract";
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ExploreDatabaseMigrator.MigrateAsync(context, configuration));

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = "Everything";
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ExploreDatabaseMigrator.MigrateAsync(context, configuration));
        });
    }

    [Test]
    [Arguments(EventLocationPrivacyMigrationStage.Expand)]
    [Arguments(EventLocationPrivacyMigrationStage.Backfill)]
    public async Task ReRequestingAppliedStage_IsSuccessfulNoOp_AndPreservesMigrationHistory(string stage)
    {
        await WithDatabaseAsync(async context =>
        {
            if (stage == EventLocationPrivacyMigrationStage.Backfill)
            {
                await EventLocationPrivacyMigrationStage.MigrateAsync(
                    context,
                    EventLocationPrivacyMigrationStage.Expand);
            }

            await EventLocationPrivacyMigrationStage.MigrateAsync(context, stage);
            string[] historyBeforeRetry = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            string expectedTarget = stage == EventLocationPrivacyMigrationStage.Expand
                ? EventLocationPrivacyMigrationStage.ExpandMigration
                : BackfillMigration;
            await Assert.That(historyBeforeRetry[^1]).IsEqualTo(expectedTarget);

            await EventLocationPrivacyMigrationStage.MigrateAsync(context, stage);

            string[] historyAfterRetry = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            await Assert.That(historyAfterRetry).IsEquivalentTo(historyBeforeRetry);
        });
    }

    [Test]
    public async Task FreshExpand_CreatesSeededAdditiveSchemaAndDatabaseGuards()
    {
        await WithDatabaseAsync(async context =>
        {
            await EventLocationPrivacyMigrationStage.MigrateAsync(context, "Expand");

            int lookupCount = await context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*)::integer AS \"Value\" FROM location_kinds")
                .SingleAsync();
            int carrierColumnCount = await context.Database.SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*)::integer AS "Value"
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND column_name = 'event_location_id'
                      AND table_name IN ('event_sessions', 'event_session_groups', 'event_agenda_items', 'event_session_agenda_items')
                    """)
                .SingleAsync();
            int triggerCount = await context.Database.SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*)::integer AS "Value"
                    FROM pg_trigger
                    WHERE NOT tgisinternal
                      AND tgrelid IN (
                          'event_locations'::regclass,
                          'event_location_disclosure_audits'::regclass,
                          'event_location_exact_read_audits'::regclass,
                          'location_privacy_erasure_replay_checkpoints'::regclass,
                          'locations'::regclass,
                          'location_pii'::regclass,
                          'location_rooms'::regclass,
                          'event_sessions'::regclass,
                          'event_session_groups'::regclass,
                          'event_agenda_items'::regclass,
                          'event_session_agenda_items'::regclass)
                    """)
                .SingleAsync();

            await Assert.That(lookupCount).IsEqualTo(5);
            await Assert.That(carrierColumnCount).IsEqualTo(4);
            await Assert.That(triggerCount).IsGreaterThanOrEqualTo(6);
        });
    }

    [Test]
    public async Task LegacyUpgrade_PreservesRows_AndDownRestoresPreviousSchema()
    {
        await WithDatabaseAsync(async context =>
        {
            IMigrator migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            Guid tenantId = Guid.CreateVersion7();
            Guid locationId = Guid.CreateVersion7();
            await context.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO tenant_statuses (id, master_code, full_name, is_active_state)
                VALUES (1, 'ACTIVE', 'Active', true)
                ON CONFLICT (id) DO NOTHING;
                INSERT INTO tenants (id, full_name, slug, tenant_status_id, created_at)
                VALUES ({{tenantId}}, 'Legacy tenant', {{"legacy-" + tenantId.ToString("N")}}, 1, now());
                INSERT INTO locations (id, full_name, country, city, tenant_id, concurrency_stamp, created_at)
                VALUES ({{locationId}}, 'Legacy venue', 'BE', 'Brussels', {{tenantId}}, {{Guid.CreateVersion7()}}, now());
                """);

            await EventLocationPrivacyMigrationStage.MigrateAsync(context, "Expand");

            int[] privacyDefaults = await context.Database.SqlQuery<int>(
                    $"SELECT location_kind_id AS \"Value\" FROM locations WHERE id = {locationId} UNION ALL SELECT location_privacy_state_id AS \"Value\" FROM locations WHERE id = {locationId}")
                .ToArrayAsync();
            await Assert.That(privacyDefaults).IsEquivalentTo([1, 1]);

            await migrator.MigrateAsync(PreviousMigration);

            int preserved = await context.Database.SqlQuery<int>(
                    $"SELECT COUNT(*)::integer AS \"Value\" FROM locations WHERE id = {locationId}")
                .SingleAsync();
            int removedColumns = await context.Database.SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*)::integer AS "Value"
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'locations'
                      AND column_name IN ('location_kind_id', 'location_privacy_state_id', 'owner_user_id')
                    """)
                .SingleAsync();

            await Assert.That(preserved).IsEqualTo(1);
            await Assert.That(removedColumns).IsEqualTo(0);
        });
    }

    [Test]
    public async Task ExpandTriggers_RejectAuditMutationErasureResurrectionAndAllCarrierMismatches()
    {
        await WithDatabaseAsync(async context =>
        {
            await EventLocationPrivacyMigrationStage.MigrateAsync(context, "Expand");
            await SeedLegacyEventLocationLookupsAsync(context);
            context.EnableTenantFilterBypass("Event Location Privacy migration trigger verification.");

            PrivacyGraph graph = await SeedGraphAsync(context);

            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
                $"DELETE FROM event_location_disclosure_audits WHERE id = '{graph.AuditId:D}'::uuid"));

            Location home = await context.Locations
                .Include(location => location.Rooms)
                .SingleAsync(location => location.Id == graph.HomeLocationId);
            home.EraseOwnedPii(DateTime.UtcNow, LocationPrivacyErasureReasonEnum.AccountDeletion);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
                $"INSERT INTO location_pii (location_id, address, postcode) VALUES ('{graph.HomeLocationId:D}'::uuid, 'restored', 'restored')"));
            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
                $"UPDATE locations SET location_privacy_state_id = 1 WHERE id = '{graph.HomeLocationId:D}'::uuid"));
            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
                $"UPDATE location_rooms SET name = 'restored' WHERE id = '{graph.HomeRoomId:D}'::uuid"));

            foreach (string table in new[]
                     {
                         "event_sessions",
                         "event_session_groups",
                         "event_agenda_items",
                         "event_session_agenda_items"
                     })
            {
                Guid carrierId = graph.CarrierIds[table];
                await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
                    $"UPDATE {table} SET location_id = '{graph.OtherLocationId:D}'::uuid WHERE id = '{carrierId:D}'::uuid"));
            }

            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
                $"UPDATE event_locations SET is_deleted = true WHERE id = '{graph.EventLocationId:D}'::uuid"));
        });
    }

    [Test]
    public async Task BackfillDown_RejectsLaterCarrierReferenceBeforeMutation_ThenRestoresOnlyCapturedCarrier()
    {
        await WithDatabaseAsync(async context =>
        {
            IMigrator migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(BackfillPreviousMigration);
            await SeedLegacyEventLocationLookupsAsync(context);
            context.EnableTenantFilterBypass("Event Location Privacy Backfill rollback verification.");

            LegacyCarrierGraph graph = await SeedLegacyCarrierGraphAsync(context);
            await migrator.MigrateAsync(BackfillMigration);
            context.ChangeTracker.Clear();

            EventLocation authority = await context.EventLocations
                .IgnoreQueryFilters()
                .SingleAsync(item => item.EventId == graph.EventId && item.LocationId == graph.LocationId);
            var laterSession = new EventSession
            {
                Id = Guid.CreateVersion7(),
                TenantId = graph.TenantId,
                Tenant = null!,
                EventId = graph.EventId,
                Event = null!,
                CreatedBy = graph.UserId
            };
            laterSession.AssignEventLocation(authority);
            context.EventSessions.Add(laterSession);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                migrator.MigrateAsync(BackfillPreviousMigration));
            var postgresFailure = failure!.GetBaseException() as PostgresException;
            await Assert.That(postgresFailure).IsNotNull();
            await Assert.That(postgresFailure!.SqlState).IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);

            Guid?[] referencesAfterRejectedDown = await context.Database.SqlQuery<Guid?>(
                    $"SELECT event_location_id AS \"Value\" FROM event_sessions WHERE id IN ({graph.LegacySessionId}, {laterSession.Id}) ORDER BY id")
                .ToArrayAsync();
            int authorityCountAfterRejectedDown = await context.Database.SqlQuery<int>(
                    $"SELECT COUNT(*)::integer AS \"Value\" FROM event_locations WHERE id = {authority.Id}")
                .SingleAsync();
            await Assert.That(referencesAfterRejectedDown).IsEquivalentTo(new Guid?[] { authority.Id, authority.Id });
            await Assert.That(authorityCountAfterRejectedDown).IsEqualTo(1);
            await Assert.That(await context.Database.GetAppliedMigrationsAsync()).Contains(BackfillMigration);

            await context.Database.ExecuteSqlAsync($"DELETE FROM event_sessions WHERE id = {laterSession.Id}");
            await migrator.MigrateAsync(BackfillPreviousMigration);

            Guid? legacyReferenceAfterSafeDown = await context.Database.SqlQuery<Guid?>(
                    $"SELECT event_location_id AS \"Value\" FROM event_sessions WHERE id = {graph.LegacySessionId}")
                .SingleAsync();
            int authorityCountAfterSafeDown = await context.Database.SqlQuery<int>(
                    $"SELECT COUNT(*)::integer AS \"Value\" FROM event_locations WHERE id = {authority.Id}")
                .SingleAsync();
            await Assert.That(legacyReferenceAfterSafeDown).IsNull();
            await Assert.That(authorityCountAfterSafeDown).IsEqualTo(0);

            await migrator.MigrateAsync(BackfillMigration);
            Guid? repairedReference = await context.Database.SqlQuery<Guid?>(
                    $"SELECT event_location_id AS \"Value\" FROM event_sessions WHERE id = {graph.LegacySessionId}")
                .SingleAsync();
            await Assert.That(repairedReference).IsNotNull();
        });
    }

    private async Task WithDatabaseAsync(Func<ExploreDbContext, Task> action)
    {
        string databaseName = $"elp_expand_{Guid.NewGuid():N}";
        string connectionString = await CreateDatabaseAsync(databaseName);
        try
        {
            await using var context = CreateContext(connectionString);
            await action(context);
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    private async Task<string> CreateDatabaseAsync(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        await command.ExecuteNonQueryAsync();
        builder.Database = databaseName;
        return builder.ConnectionString;
    }

    private async Task DropDatabaseAsync(string databaseName)
    {
        NpgsqlConnection.ClearAllPools();
        var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var terminate = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()",
            connection);
        terminate.Parameters.AddWithValue("database", databaseName);
        await terminate.ExecuteNonQueryAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", connection);
        await drop.ExecuteNonQueryAsync();
    }

    private static ExploreDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new ExploreDbContext(options);
    }

    private static Task<int> SeedLegacyEventLocationLookupsAsync(ExploreDbContext context)
    {
        return context.Database.ExecuteSqlRawAsync("""
            INSERT INTO tenant_statuses (id, master_code, full_name, description, is_active_state)
            VALUES (2, 'ACTIVE', 'Active', 'Tenant is active and operational', true)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO actor_types (id, master_code, full_name, description)
            VALUES (1, 'USER', 'User', 'Individual user actor')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO event_statuses (id, master_code, full_name, description)
            VALUES (1, 'DRAFT', 'Draft', 'Event is in draft state and not visible to the public')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO event_formats (id, master_code, full_name, description)
            VALUES (1, 'LOCAL', 'Local (In-Person)', 'Event takes place at a physical location')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO visibility_types (id, master_code, full_name, description)
            VALUES (1, 'PUBLIC', 'Public', 'Visible to everyone')
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    private static async Task<PrivacyGraph> SeedGraphAsync(ExploreDbContext context)
    {
        var tenant = new Tenant
        {
            FullName = "ELP migration trigger tenant",
            Slug = $"elp-trigger-{Guid.NewGuid():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        var user = new User
        {
            Pii = new UserPii
            {
                Email = $"elp-trigger-{Guid.NewGuid():N}@example.com",
                FirstName = "Privacy",
                LastName = "Owner"
            }
        };
        context.AddRange(tenant, user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = "ELP migration trigger actor" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var eventEntity = new Explore.Domain.Event
        {
            TenantId = tenant.Id,
            Tenant = null!,
            ActorId = actor.Id,
            Actor = null!,
            Title = "ELP migration trigger event",
            EventStatusId = (int)EventStatusEnum.Draft,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            IsRegistrationRequired = false
        };
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            FullName = "Physical venue",
            Country = "BE",
            City = "Brussels"
        };
        var otherLocation = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            FullName = "Other venue",
            Country = "BE",
            City = "Ghent"
        };
        var home = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            FullName = "Owner home",
            Country = "BE",
            City = "Brussels"
        };
        home.ClassifyAsPrivateHome(user.Id);
        home.AttachPii(new LocationPii
        {
            LocationId = home.Id,
            Address = "Private street 1",
            Postcode = "1000"
        });
        var homeRoom = new LocationRoom
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            LocationId = home.Id,
            Location = home,
            Name = "Living room"
        };
        home.Rooms.Add(homeRoom);
        context.AddRange(eventEntity, location, otherLocation, home, homeRoom);
        await context.SaveChangesAsync();

        EventLocation eventLocation = EventLocation.CreatePhysical(
            tenant.Id,
            eventEntity.Id,
            location.Id,
            user.Id,
            DateTime.UtcNow);
        EventLocationDisclosureAudit audit = eventLocation.CreateInitialDisclosureAudit();
        context.AddRange(eventLocation, audit);
        await context.SaveChangesAsync();

        var session = new EventSession
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            EventId = eventEntity.Id,
            Event = null!
        };
        session.AssignEventLocation(eventLocation);
        var group = new EventSessionGroup
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            EventId = eventEntity.Id,
            Event = null!,
            Name = "ELP trigger group"
        };
        group.AssignEventLocation(eventLocation);
        var agenda = new EventAgendaItem
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            EventId = eventEntity.Id,
            Event = null!,
            Title = "ELP trigger agenda",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1)
        };
        agenda.ReprojectLocalTimes("UTC", new EventScheduleProjectionCalculator());
        agenda.AssignEventLocation(eventLocation);
        var sessionAgenda = new EventSessionAgendaItem
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            EventSessionId = session.Id,
            EventSession = session,
            Title = "ELP trigger session agenda",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        sessionAgenda.AssignEventLocation(eventLocation);
        context.AddRange(session, group, agenda, sessionAgenda);
        await context.SaveChangesAsync();

        return new PrivacyGraph(
            eventLocation.Id,
            audit.Id,
            home.Id,
            homeRoom.Id,
            otherLocation.Id,
            new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                ["event_sessions"] = session.Id,
                ["event_session_groups"] = group.Id,
                ["event_agenda_items"] = agenda.Id,
                ["event_session_agenda_items"] = sessionAgenda.Id
            });
    }

    private static async Task<LegacyCarrierGraph> SeedLegacyCarrierGraphAsync(ExploreDbContext context)
    {
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = "ELP Backfill rollback tenant",
            Slug = $"elp-backfill-{Guid.NewGuid():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"elp-backfill-{Guid.NewGuid():N}@example.com",
                FirstName = "Backfill",
                LastName = "Owner"
            }
        };
        context.AddRange(tenant, user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = "ELP Backfill rollback actor" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var eventEntity = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            ActorId = actor.Id,
            Actor = null!,
            Title = "ELP Backfill rollback event",
            EventStatusId = (int)EventStatusEnum.Draft,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            IsRegistrationRequired = false,
            CreatedBy = user.Id
        };
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            FullName = "ELP Backfill legacy venue",
            Country = "BE",
            City = "Brussels"
        };
        context.AddRange(eventEntity, location);
        await context.SaveChangesAsync();

        var legacySession = new EventSession
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            EventId = eventEntity.Id,
            Event = null!,
            LocationId = location.Id,
            CreatedBy = user.Id
        };
        context.EventSessions.Add(legacySession);
        await context.SaveChangesAsync();

        return new LegacyCarrierGraph(tenant.Id, user.Id, eventEntity.Id, location.Id, legacySession.Id);
    }

    private sealed record PrivacyGraph(
        Guid EventLocationId,
        Guid AuditId,
        Guid HomeLocationId,
        Guid HomeRoomId,
        Guid OtherLocationId,
        IReadOnlyDictionary<string, Guid> CarrierIds);

    private sealed record LegacyCarrierGraph(
        Guid TenantId,
        Guid UserId,
        Guid EventId,
        Guid LocationId,
        Guid LegacySessionId);
}
