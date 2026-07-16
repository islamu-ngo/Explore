// ABOUTME: PostgreSQL acceptance tests for the Event Location Privacy Expand migration and stage gate.
// ABOUTME: Verifies fresh/legacy rollout, rollback, immutable privacy evidence, and carrier consistency.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using Explore.Persistence.Schema;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
[Property("Category", "EventLocationPrivacy")]
public sealed class EventLocationMigrationStageTests(PostgreSqlContainerFixture fixture)
{
    private const string PreviousMigration = "20260715172404_AddTypedWebhookOwnership";

    [Test]
    public async Task GenericMigrator_RequiresExplicitPendingStage_AndRejectsUnavailableContract()
    {
        await WithDatabaseAsync(async context =>
        {
            var configuration = new ConfigurationManager();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ExploreDatabaseMigrator.MigrateAsync(context, configuration));

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = "Contract";
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ExploreDatabaseMigrator.MigrateAsync(context, configuration));

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = "Expand";
            await ExploreDatabaseMigrator.MigrateAsync(context, configuration);

            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey] = null;
            await ExploreDatabaseMigrator.MigrateAsync(context, configuration);

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

            int[] privacyDefaults = await context.Database.SqlQueryRaw<int>(
                    $"SELECT location_kind_id AS \"Value\" FROM locations WHERE id = '{locationId:D}'::uuid UNION ALL SELECT location_privacy_state_id AS \"Value\" FROM locations WHERE id = '{locationId:D}'::uuid")
                .ToArrayAsync();
            await Assert.That(privacyDefaults).IsEquivalentTo([1, 1]);

            await migrator.MigrateAsync(PreviousMigration);

            int preserved = await context.Database.SqlQueryRaw<int>(
                    $"SELECT COUNT(*)::integer AS \"Value\" FROM locations WHERE id = '{locationId:D}'::uuid")
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
            await LookupTableSeeder.SeedAsync(context);
            context.EnableTenantFilterBypass("Event Location Privacy migration trigger verification.");

            PrivacyGraph graph = await SeedGraphAsync(context);

            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM event_location_disclosure_audits WHERE id = '{graph.AuditId:D}'::uuid"));

            Location home = await context.Locations
                .Include(location => location.Rooms)
                .SingleAsync(location => location.Id == graph.HomeLocationId);
            home.EraseOwnedPii(DateTime.UtcNow, LocationPrivacyErasureReasonEnum.AccountDeletion);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO location_pii (location_id, address, postcode) VALUES ('{graph.HomeLocationId:D}'::uuid, 'restored', 'restored')"));
            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
                $"UPDATE locations SET location_privacy_state_id = 1 WHERE id = '{graph.HomeLocationId:D}'::uuid"));
            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
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
                await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
                    $"UPDATE {table} SET location_id = '{graph.OtherLocationId:D}'::uuid WHERE id = '{carrierId:D}'::uuid"));
            }

            await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
                $"UPDATE event_locations SET is_deleted = true WHERE id = '{graph.EventLocationId:D}'::uuid"));
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

    private sealed record PrivacyGraph(
        Guid EventLocationId,
        Guid AuditId,
        Guid HomeLocationId,
        Guid HomeRoomId,
        Guid OtherLocationId,
        IReadOnlyDictionary<string, Guid> CarrierIds);
}
