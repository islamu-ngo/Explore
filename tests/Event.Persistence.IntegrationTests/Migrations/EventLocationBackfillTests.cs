// ABOUTME: PostgreSQL acceptance tests for the conservative Event Location Privacy legacy backfill.
// ABOUTME: Proves four-carrier convergence, repeat safety, rollback guards, and forward repair.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using Explore.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
[Property("Category", "EventLocationPrivacy")]
public sealed class EventLocationBackfillTests(RecipientDeliveryMigrationContainerFixture fixture)
{
    private const string PreviousMigration = "20260718210538_HardenAtprotoFederationPersistence";
    private const string BackfillMigration = "20260718215537_BackfillUnclassifiedEventLocations";

    [Test]
    public async Task RepresentativeLegacyRows_ConvergeAcrossAllCarriers_AndReplayKeepsStableAuthority()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            ReadCarrierAuthorityAsync(null!, "events", Guid.Empty));

        await WithDatabaseAsync(async context =>
        {
            IMigrator migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            await SeedRequiredLookupsAsync(context);
            context.EnableTenantFilterBypass("ELP-230B representative legacy acceptance.");
            BackfillGraph graph = await SeedBackfillGraphAsync(context);

            await migrator.MigrateAsync(BackfillMigration);

            await AssertBackfillConvergedAsync(context, graph, expectedBackfilledAuthorities: 3);
            string firstHash = await ReadBackfillHashAsync(context);
            Guid existingPhysicalId = await ReadCarrierAuthorityAsync(context, "event_sessions", graph.ExistingPhysicalCarrierId);
            Guid existingTbaId = await ReadCarrierAuthorityAsync(context, "event_session_groups", graph.ExistingTbaCarrierId);

            await ReplayBackfillSqlAsync(context);

            await Assert.That(await ReadBackfillHashAsync(context)).IsEqualTo(firstHash);
            await Assert.That(await ReadCarrierAuthorityAsync(context, "event_sessions", graph.ExistingPhysicalCarrierId))
                .IsEqualTo(existingPhysicalId);
            await Assert.That(await ReadCarrierAuthorityAsync(context, "event_session_groups", graph.ExistingTbaCarrierId))
                .IsEqualTo(existingTbaId);
            await AssertBackfillConvergedAsync(context, graph, expectedBackfilledAuthorities: 3);

            Guid laterEventId = await SeedLaterLegacySessionAsync(context, graph);
            await ReplayBackfillSqlAsync(context);

            int laterAuthorityCount = await ScalarIntAsync(context, $"""
                SELECT COUNT(*)::integer AS "Value"
                FROM event_locations
                WHERE tenant_id = '{graph.TenantId:D}'::uuid
                  AND event_id = '{laterEventId:D}'::uuid
                  AND location_id = '{graph.NoPiiLocationId:D}'::uuid
                  AND is_deleted = false
                """);
            await Assert.That(laterAuthorityCount).IsEqualTo(1);

            string repairedHash = await ReadBackfillHashAsync(context);
            await ReplayBackfillSqlAsync(context);
            await Assert.That(await ReadBackfillHashAsync(context)).IsEqualTo(repairedHash);
            await Assert.That(await ReadCarrierAuthorityAsync(context, "event_sessions", graph.ExistingPhysicalCarrierId))
                .IsEqualTo(existingPhysicalId);
            await Assert.That(await ReadCarrierAuthorityAsync(context, "event_session_groups", graph.ExistingTbaCarrierId))
                .IsEqualTo(existingTbaId);
            await AssertZeroGapsAsync(context);
        });
    }

    [Test]
    public async Task MalformedLegacyCarrier_FailsAtomically_ThenForwardRepairConverges()
    {
        await WithDatabaseAsync(async context =>
        {
            IMigrator migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            await SeedRequiredLookupsAsync(context);
            context.EnableTenantFilterBypass("ELP-230B malformed legacy acceptance.");
            BackfillGraph graph = await SeedBackfillGraphAsync(context);

            await context.Database.ExecuteSqlRawAsync("ALTER TABLE event_sessions DISABLE TRIGGER ALL");
            await context.Database.ExecuteSqlAsync($"""
                UPDATE event_sessions
                SET tenant_id = {graph.OtherTenantId}
                WHERE id = {graph.DirectSessionId}
                """);
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE event_sessions ENABLE TRIGGER ALL");

            Exception failure = await CaptureFailureAsync(() => migrator.MigrateAsync(BackfillMigration));
            var postgresFailure = failure.GetBaseException() as PostgresException;
            await Assert.That(postgresFailure).IsNotNull();
            await Assert.That(postgresFailure!.SqlState).IsEqualTo(PostgresErrorCodes.CheckViolation);
            await Assert.That(await context.Database.GetAppliedMigrationsAsync()).DoesNotContain(BackfillMigration);
            await Assert.That(await ScalarIntAsync(context, $"""
                SELECT location_privacy_state_id AS "Value"
                FROM locations
                WHERE id = '{graph.PiiLocationId:D}'::uuid
                """)).IsEqualTo((int)LocationPrivacyStateEnum.NotProvided);
            await Assert.That(await ScalarIntAsync(context,
                "SELECT COUNT(*)::integer AS \"Value\" FROM event_locations WHERE last_policy_actor_user_id IS NOT NULL"))
                .IsEqualTo(2);

            await context.Database.ExecuteSqlRawAsync("ALTER TABLE event_sessions DISABLE TRIGGER ALL");
            await context.Database.ExecuteSqlAsync($"""
                UPDATE event_sessions
                SET tenant_id = {graph.TenantId}
                WHERE id = {graph.DirectSessionId}
                """);
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE event_sessions ENABLE TRIGGER ALL");

            await migrator.MigrateAsync(BackfillMigration);
            await AssertBackfillConvergedAsync(context, graph, expectedBackfilledAuthorities: 3);
        });
    }

    [Test]
    public async Task Down_RejectsPostWritePolicyChangeBeforeMutation_ThenSafeDownAndForwardConverge()
    {
        await WithDatabaseAsync(async context =>
        {
            IMigrator migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            await SeedRequiredLookupsAsync(context);
            context.EnableTenantFilterBypass("ELP-230B guarded rollback acceptance.");
            BackfillGraph graph = await SeedBackfillGraphAsync(context);
            await migrator.MigrateAsync(BackfillMigration);

            Guid authorityId = await ReadCarrierAuthorityAsync(context, "event_sessions", graph.DirectSessionId);
            string beforeRejectedDown = await ReadBackfillHashAsync(context);
            await context.Database.ExecuteSqlAsync($"""
                UPDATE event_locations SET show_city = true WHERE id = {authorityId}
                """);

            Exception failure = await CaptureFailureAsync(() => migrator.MigrateAsync(PreviousMigration));
            var postgresFailure = failure.GetBaseException() as PostgresException;
            await Assert.That(postgresFailure).IsNotNull();
            await Assert.That(postgresFailure!.SqlState)
                .IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await context.Database.GetAppliedMigrationsAsync()).Contains(BackfillMigration);
            await Assert.That(await ReadBackfillHashAsync(context)).IsNotEqualTo(beforeRejectedDown);
            await Assert.That(await ScalarIntAsync(context, $"""
                SELECT COUNT(*)::integer AS "Value" FROM event_locations WHERE id = '{authorityId:D}'::uuid
                """)).IsEqualTo(1);

            await context.Database.ExecuteSqlAsync($"""
                UPDATE event_locations SET show_city = false WHERE id = {authorityId}
                """);
            await migrator.MigrateAsync(PreviousMigration);

            await Assert.That(await ScalarIntAsync(context,
                "SELECT COUNT(*)::integer AS \"Value\" FROM event_location_disclosure_audits WHERE reason = 5"))
                .IsEqualTo(0);
            await Assert.That(await ScalarIntAsync(context, """
                SELECT COUNT(*)::integer AS "Value"
                FROM event_sessions
                WHERE event_location_id IS NOT NULL
                """)).IsEqualTo(1);
            await Assert.That(await ReadCarrierAuthorityAsync(context, "event_sessions", graph.ExistingPhysicalCarrierId))
                .IsEqualTo(graph.ExistingPhysicalAuthorityId);

            await migrator.MigrateAsync(BackfillMigration);
            await AssertBackfillConvergedAsync(context, graph, expectedBackfilledAuthorities: 3);
        });
    }

    private static async Task AssertBackfillConvergedAsync(
        ExploreDbContext context,
        BackfillGraph graph,
        int expectedBackfilledAuthorities)
    {
        await Assert.That(await ScalarIntAsync(context, """
            SELECT COUNT(*)::integer AS "Value"
            FROM event_locations authority
            WHERE authority.is_deleted = false
              AND authority.is_to_be_announced = false
              AND authority.location_id IS NOT NULL
              AND authority.last_policy_actor_user_id IS NOT NULL
            """)).IsEqualTo(expectedBackfilledAuthorities + 1);
        await Assert.That(await ScalarIntAsync(context, """
            SELECT COUNT(*)::integer AS "Value"
            FROM (
                SELECT tenant_id, event_id, location_id
                FROM event_locations
                WHERE is_deleted = false AND is_to_be_announced = false
                GROUP BY tenant_id, event_id, location_id
                HAVING COUNT(*) <> 1
            ) duplicate
            """)).IsEqualTo(0);
        await Assert.That(await ScalarIntAsync(context, """
            SELECT COUNT(*)::integer AS "Value"
            FROM event_locations authority
            INNER JOIN event_location_disclosure_audits audit
              ON audit.tenant_id = authority.tenant_id
             AND audit.event_location_id = authority.id
            WHERE audit.reason = 5
              AND audit.previous_fields = 0
              AND audit.new_fields = 4
              AND audit.previous_audience_id = 1
              AND audit.new_audience_id = 1
              AND audit.previous_policy_version = 0
              AND audit.new_policy_version = 1
              AND authority.show_venue_name = false
              AND authority.show_city = false
              AND authority.show_country = true
              AND authority.show_room_name = false
              AND authority.show_street_address = false
              AND authority.show_postcode = false
              AND authority.show_coordinates = false
              AND authority.full_details_audience_id = 1
              AND authority.reveal_full_details_from_utc IS NULL
              AND authority.needs_privacy_review = true
              AND authority.policy_version = 1
              AND authority.is_to_be_announced = false
              AND authority.is_deleted = false
            """)).IsEqualTo(expectedBackfilledAuthorities);
        await Assert.That(await ScalarIntAsync(context, """
            SELECT COUNT(*)::integer AS "Value"
            FROM event_location_disclosure_audits audit
            WHERE audit.reason = 5
              AND (SELECT COUNT(*) FROM event_location_disclosure_audits sibling
                   WHERE sibling.event_location_id = audit.event_location_id) <> 1
            """)).IsEqualTo(0);
        await Assert.That(await ScalarIntAsync(context, """
            SELECT COUNT(*)::integer AS "Value"
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'event_location_disclosure_audits'
              AND column_name IN ('full_name', 'address', 'postcode', 'city', 'country', 'latitude', 'longitude')
            """)).IsEqualTo(0);
        await Assert.That(await ScalarIntAsync(context, $"""
            SELECT COUNT(*)::integer AS "Value"
            FROM locations
            WHERE id IN ('{graph.PiiLocationId:D}'::uuid, '{graph.NoPiiLocationId:D}'::uuid)
              AND location_kind_id = {(int)LocationKindEnum.Unclassified}
              AND owner_user_id IS NULL
              AND pii_erased_at_utc IS NULL
              AND pii_erasure_reason IS NULL
            """)).IsEqualTo(2);
        await Assert.That(await ScalarIntAsync(context, $"""
            SELECT location_privacy_state_id AS "Value"
            FROM locations WHERE id = '{graph.PiiLocationId:D}'::uuid
            """)).IsEqualTo((int)LocationPrivacyStateEnum.Active);
        await Assert.That(await ScalarIntAsync(context, $"""
            SELECT location_privacy_state_id AS "Value"
            FROM locations WHERE id = '{graph.NoPiiLocationId:D}'::uuid
            """)).IsEqualTo((int)LocationPrivacyStateEnum.NotProvided);
        await Assert.That(await ScalarIntAsync(context, $"""
            SELECT location_kind_id AS "Value"
            FROM locations WHERE id = '{graph.ExistingPhysicalLocationId:D}'::uuid
            """)).IsEqualTo((int)LocationKindEnum.CommercialVenue);
        await Assert.That(await ReadCarrierAuthorityAsync(context, "event_sessions", graph.ExistingPhysicalCarrierId))
            .IsEqualTo(graph.ExistingPhysicalAuthorityId);
        await Assert.That(await ReadCarrierAuthorityAsync(context, "event_session_groups", graph.ExistingTbaCarrierId))
            .IsEqualTo(graph.ExistingTbaAuthorityId);
        await AssertZeroGapsAsync(context);
    }

    private static async Task AssertZeroGapsAsync(ExploreDbContext context)
    {
        await Assert.That(await ScalarIntAsync(context, """
            SELECT COUNT(*)::integer AS "Value"
            FROM (
                SELECT tenant_id, event_id, location_id, room_id, event_location_id FROM event_sessions
                UNION ALL
                SELECT tenant_id, event_id, location_id, room_id, event_location_id FROM event_session_groups
                UNION ALL
                SELECT tenant_id, event_id, location_id, room_id, event_location_id FROM event_agenda_items
            ) carrier
            LEFT JOIN event_locations authority
              ON authority.tenant_id = carrier.tenant_id
             AND authority.event_id = carrier.event_id
             AND authority.id = carrier.event_location_id
             AND authority.location_id IS NOT DISTINCT FROM carrier.location_id
             AND authority.is_deleted = false
            LEFT JOIN location_rooms room
              ON room.tenant_id = carrier.tenant_id
             AND room.id = carrier.room_id
            WHERE carrier.location_id IS NOT NULL
              AND (authority.id IS NULL OR (carrier.room_id IS NOT NULL AND room.location_id <> carrier.location_id))
            """)).IsEqualTo(0);
        await Assert.That(await ScalarIntAsync(context, """
            SELECT COUNT(*)::integer AS "Value"
            FROM event_session_agenda_items carrier
            INNER JOIN event_sessions parent
              ON parent.tenant_id = carrier.tenant_id
             AND parent.id = carrier.event_session_id
            LEFT JOIN event_locations authority
              ON authority.tenant_id = carrier.tenant_id
             AND authority.event_id = parent.event_id
             AND authority.id = carrier.event_location_id
             AND authority.location_id IS NOT DISTINCT FROM carrier.location_id
             AND authority.is_deleted = false
            WHERE carrier.location_id IS NOT NULL AND authority.id IS NULL
            """)).IsEqualTo(0);
    }

    private static async Task<string> ReadBackfillHashAsync(ExploreDbContext context)
    {
        return await context.Database.SqlQueryRaw<string>("""
                SELECT md5(string_agg(value, '|' ORDER BY value)) AS "Value"
                FROM (
                    SELECT concat_ws(':', 'authority', id, event_id, location_id, show_city, show_country, policy_version)
                    FROM event_locations
                    UNION ALL
                    SELECT concat_ws(':', 'audit', id, event_location_id, previous_fields, new_fields, reason)
                    FROM event_location_disclosure_audits
                    UNION ALL
                    SELECT concat_ws(':', 'session', id, location_id, event_location_id) FROM event_sessions
                    UNION ALL
                    SELECT concat_ws(':', 'group', id, location_id, event_location_id) FROM event_session_groups
                    UNION ALL
                    SELECT concat_ws(':', 'agenda', id, location_id, event_location_id) FROM event_agenda_items
                    UNION ALL
                    SELECT concat_ws(':', 'session_agenda', id, location_id, event_location_id) FROM event_session_agenda_items
                ) stable(value)
                """)
            .SingleAsync();
    }

    private static async Task<Guid> ReadCarrierAuthorityAsync(
        ExploreDbContext context,
        string table,
        Guid carrierId)
    {
        string sql = table switch
        {
            "event_sessions" => """SELECT event_location_id AS "Value" FROM event_sessions WHERE id = @carrierId""",
            "event_session_groups" => """SELECT event_location_id AS "Value" FROM event_session_groups WHERE id = @carrierId""",
            "event_agenda_items" => """SELECT event_location_id AS "Value" FROM event_agenda_items WHERE id = @carrierId""",
            "event_session_agenda_items" => """SELECT event_location_id AS "Value" FROM event_session_agenda_items WHERE id = @carrierId""",
            _ => throw new ArgumentOutOfRangeException(nameof(table), table, "Unsupported carrier table.")
        };

        return await context.Database.SqlQueryRaw<Guid>(sql, new NpgsqlParameter("carrierId", carrierId))
            .SingleAsync();
    }

    private static async Task ReplayBackfillSqlAsync(ExploreDbContext context)
    {
        var migration = new BackfillProbe();
        await using var transaction = await context.Database.BeginTransactionAsync();
        foreach (SqlOperation operation in migration.BuildUp().OfType<SqlOperation>())
        {
            await context.Database.ExecuteSqlRawAsync(operation.Sql);
        }

        await transaction.CommitAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task<int> ScalarIntAsync(ExploreDbContext context, string sql)
    {
        return await context.Database.SqlQueryRaw<int>(sql).SingleAsync();
    }

    private static async Task<Exception> CaptureFailureAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("Expected operation to fail.");
    }

    private async Task WithDatabaseAsync(Func<ExploreDbContext, Task> action)
    {
        string databaseName = $"elp_backfill_{Guid.NewGuid():N}";
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

    private static Task<int> SeedRequiredLookupsAsync(ExploreDbContext context)
    {
        return context.Database.ExecuteSqlRawAsync("""
            INSERT INTO tenant_statuses (id, master_code, full_name, description, is_active_state)
            VALUES (2, 'ACTIVE', 'Active', 'Tenant is active and operational', true)
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO actor_types (id, master_code, full_name, description)
            VALUES (1, 'USER', 'User', 'Individual user actor')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO event_statuses (id, master_code, full_name, description)
            VALUES (1, 'DRAFT', 'Draft', 'Event is in draft state')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO event_formats (id, master_code, full_name, description)
            VALUES (1, 'LOCAL', 'Local', 'Physical event')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO visibility_types (id, master_code, full_name, description)
            VALUES (1, 'PUBLIC', 'Public', 'Visible to everyone')
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    private static async Task<BackfillGraph> SeedBackfillGraphAsync(ExploreDbContext context)
    {
        var tenant = CreateTenant("legacy");
        var otherTenant = CreateTenant("other");
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"elp-backfill-{Guid.NewGuid():N}@example.test",
                FirstName = "Legacy",
                LastName = "Owner"
            }
        };
        context.AddRange(tenant, otherTenant, user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = "ELP backfill actor" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        Explore.Domain.Event eventOne = CreateEvent(tenant.Id, actor.Id, user.Id, "Legacy event one");
        Explore.Domain.Event eventTwo = CreateEvent(tenant.Id, actor.Id, user.Id, "Legacy event two");
        Explore.Domain.Event dualWriteEvent = CreateEvent(tenant.Id, actor.Id, user.Id, "Dual-write event");
        var piiLocation = CreateLocation(tenant.Id, "Legacy PII venue", "Brussels");
        piiLocation.AttachPii(new LocationPii
        {
            LocationId = piiLocation.Id,
            Address = "Legacy exact address",
            Postcode = "1000"
        });
        var noPiiLocation = CreateLocation(tenant.Id, "Legacy coarse venue", "Ghent");
        var dualWriteLocation = CreateLocation(tenant.Id, "New dual-write venue", "Antwerp");
        dualWriteLocation.ClassifyAs(LocationKindEnum.CommercialVenue);
        var room = new LocationRoom
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            LocationId = noPiiLocation.Id,
            Location = noPiiLocation,
            Name = "Legacy room"
        };
        noPiiLocation.Rooms.Add(room);
        context.AddRange(eventOne, eventTwo, dualWriteEvent, piiLocation, noPiiLocation, dualWriteLocation, room);
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlAsync($"""
            UPDATE locations SET location_privacy_state_id = {(int)LocationPrivacyStateEnum.NotProvided}
            WHERE id = {piiLocation.Id};
            UPDATE locations SET location_privacy_state_id = {(int)LocationPrivacyStateEnum.Active}
            WHERE id = {noPiiLocation.Id};
            """);

        var directSession = CreateSession(tenant.Id, eventOne.Id, user.Id, piiLocation.Id);
        var secondEventSession = CreateSession(tenant.Id, eventTwo.Id, user.Id, piiLocation.Id);
        var roomGroup = new EventSessionGroup
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            EventId = eventOne.Id,
            Event = null!,
            Name = "Room-only legacy group",
            LocationId = noPiiLocation.Id,
            RoomId = room.Id,
            CreatedBy = user.Id
        };
        var roomAgenda = new EventAgendaItem
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            EventId = eventOne.Id,
            Event = null!,
            Title = "Room-only legacy agenda",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            LocationId = noPiiLocation.Id,
            RoomId = room.Id,
            CreatedBy = user.Id
        };
        roomAgenda.ReprojectLocalTimes("UTC", new EventScheduleProjectionCalculator());
        var sessionAgenda = new EventSessionAgendaItem
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            EventSessionId = directSession.Id,
            EventSession = directSession,
            Title = "Legacy session agenda",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddMinutes(30),
            LocationId = piiLocation.Id
        };

        EventLocation physicalAuthority = EventLocation.CreatePhysical(
            tenant.Id,
            dualWriteEvent.Id,
            dualWriteLocation.Id,
            user.Id,
            DateTime.UtcNow);
        EventLocation tbaAuthority = EventLocation.CreateToBeAnnounced(
            tenant.Id,
            dualWriteEvent.Id,
            user.Id,
            DateTime.UtcNow);
        EventLocationDisclosureAudit physicalAudit = physicalAuthority.CreateInitialDisclosureAudit();
        EventLocationDisclosureAudit tbaAudit = tbaAuthority.CreateInitialDisclosureAudit();
        var physicalCarrier = CreateSession(tenant.Id, dualWriteEvent.Id, user.Id, null);
        physicalCarrier.AssignEventLocation(physicalAuthority);
        var tbaCarrier = new EventSessionGroup
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            EventId = dualWriteEvent.Id,
            Event = null!,
            Name = "Existing TBA dual-write",
            CreatedBy = user.Id
        };
        tbaCarrier.AssignEventLocation(tbaAuthority);

        context.AddRange(
            directSession,
            secondEventSession,
            roomGroup,
            roomAgenda,
            sessionAgenda,
            physicalAuthority,
            tbaAuthority,
            physicalAudit,
            tbaAudit,
            physicalCarrier,
            tbaCarrier);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return new BackfillGraph(
            tenant.Id,
            otherTenant.Id,
            user.Id,
            actor.Id,
            piiLocation.Id,
            noPiiLocation.Id,
            dualWriteLocation.Id,
            directSession.Id,
            physicalCarrier.Id,
            physicalAuthority.Id,
            tbaCarrier.Id,
            tbaAuthority.Id);
    }

    private static async Task<Guid> SeedLaterLegacySessionAsync(ExploreDbContext context, BackfillGraph graph)
    {
        Explore.Domain.Event eventEntity = CreateEvent(
            graph.TenantId,
            graph.ActorId,
            graph.UserId,
            "Legacy event added between passes");
        var session = CreateSession(graph.TenantId, eventEntity.Id, graph.UserId, graph.NoPiiLocationId);
        context.AddRange(eventEntity, session);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return eventEntity.Id;
    }

    private static Tenant CreateTenant(string suffix) => new()
    {
        Id = Guid.CreateVersion7(),
        FullName = $"ELP backfill {suffix} tenant",
        Slug = $"elp-backfill-{suffix}-{Guid.NewGuid():N}"[..48],
        TenantStatusId = (int)TenantStatusEnum.Active,
        TenantStatus = null!
    };

    private static Explore.Domain.Event CreateEvent(Guid tenantId, Guid actorId, Guid userId, string title) => new()
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
        VisibilityTypeId = (int)VisibilityTypeEnum.Public,
        VisibilityType = null!,
        IsRegistrationRequired = false,
        CreatedBy = userId
    };

    private static Location CreateLocation(Guid tenantId, string name, string city) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        Tenant = null!,
        FullName = name,
        Country = "BE",
        City = city,
        CreatedAt = DateTime.UtcNow,
        ConcurrencyStamp = Guid.CreateVersion7()
    };

    private static EventSession CreateSession(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        Guid? locationId) => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            EventId = eventId,
            Event = null!,
            LocationId = locationId,
            CreatedBy = userId
        };

    private sealed class BackfillProbe : BackfillUnclassifiedEventLocations
    {
        public IReadOnlyList<MigrationOperation> BuildUp()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            Up(builder);
            return builder.Operations;
        }
    }

    private sealed record BackfillGraph(
        Guid TenantId,
        Guid OtherTenantId,
        Guid UserId,
        Guid ActorId,
        Guid PiiLocationId,
        Guid NoPiiLocationId,
        Guid ExistingPhysicalLocationId,
        Guid DirectSessionId,
        Guid ExistingPhysicalCarrierId,
        Guid ExistingPhysicalAuthorityId,
        Guid ExistingTbaCarrierId,
        Guid ExistingTbaAuthorityId);
}
