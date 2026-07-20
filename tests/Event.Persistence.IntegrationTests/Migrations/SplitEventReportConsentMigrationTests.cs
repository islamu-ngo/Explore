// ABOUTME: Rehearses reporter-consent splitting against an isolated PostgreSQL database.
// ABOUTME: Proves legacy consent maps only to follow-up and survives Up-Down-Up safely.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RecipientDeliveryMigrationDb")]
public sealed class SplitEventReportConsentMigrationTests(
    RecipientDeliveryMigrationContainerFixture fixture)
{
    private const string PreviousMigration = "20260719203000_AddNotificationFanoutProcessorState";
    private const string TargetMigration = "20260719210000_SplitEventReportConsent";
    private static readonly Guid TenantId = Guid.Parse("019f6d35-a000-7000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("019f6d35-a000-7000-8000-000000000002");
    private static readonly Guid ActorId = Guid.Parse("019f6d35-a000-7000-8000-000000000003");
    private static readonly Guid EventId = Guid.Parse("019f6d35-a000-7000-8000-000000000004");
    private static readonly Guid LegacyTrueReportId = Guid.Parse("019f6d35-a000-7000-8000-000000000005");
    private static readonly Guid LegacyFalseReportId = Guid.Parse("019f6d35-a000-7000-8000-000000000006");

    [Test]
    public async Task PopulatedUpDownUp_PreservesFollowUpWithoutWideningCaseUpdateConsent()
    {
        await using var context = CreateDbContext();
        IMigrator migrator = context.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            await SeedLegacyReportsAsync(context);

            await migrator.MigrateAsync(TargetMigration);
            await AssertConsentAsync(LegacyTrueReportId, caseUpdates: false, followUp: true);
            await AssertConsentAsync(LegacyFalseReportId, caseUpdates: false, followUp: false);

            await context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE event_reports
                SET report_case_updates_consent = true,
                    report_follow_up_contact_consent = false
                WHERE id = {LegacyTrueReportId}
                """);

            await migrator.MigrateAsync(PreviousMigration);
            await AssertLegacyConsentAsync(LegacyTrueReportId, expected: false);
            await AssertLegacyConsentAsync(LegacyFalseReportId, expected: false);

            await migrator.MigrateAsync(TargetMigration);
            await AssertConsentAsync(LegacyTrueReportId, caseUpdates: false, followUp: false);
            await AssertConsentAsync(LegacyFalseReportId, caseUpdates: false, followUp: false);
        }
        finally
        {
            await ResetSharedMigrationDatabaseAsync();
        }
    }

    private ExploreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new ExploreDbContext(options);
    }

    private static async Task SeedLegacyReportsAsync(ExploreDbContext context)
    {
        var createdAt = new DateTime(2026, 7, 19, 18, 45, 0, DateTimeKind.Utc);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tenant_statuses (id, master_code, full_name, is_active_state)
            VALUES (1, 'ACTIVE', 'Active', true)
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO tenants (id, tenant_status_id, full_name, slug, created_at)
            VALUES ({TenantId}, 1, 'Consent migration tenant', 'consent-migration-tenant', {createdAt});
            INSERT INTO users (id, concurrency_stamp, created_at, is_deleted, email_verified)
            VALUES ({UserId}, {Guid.Parse("019f6d35-a000-7000-8000-000000000007")}, {createdAt}, false, true);
            INSERT INTO actor_types (id, master_code, full_name)
            VALUES (1, 'USER', 'User')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO actors
                (id, actor_type_id, concurrency_stamp, created_at, is_deleted, tenant_id, user_id)
            VALUES
                ({ActorId}, 1, {Guid.Parse("019f6d35-a000-7000-8000-000000000008")},
                 {createdAt}, false, {TenantId}, {UserId});
            INSERT INTO event_formats (id, master_code, full_name)
            VALUES (1, 'IN_PERSON', 'In person')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO event_statuses (id, master_code, full_name)
            VALUES (1, 'DRAFT', 'Draft')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO visibility_types (id, master_code, full_name)
            VALUES (1, 'PUBLIC', 'Public')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO events
                (id, actor_id, concurrency_stamp, created_at, event_format_id, event_status_id,
                 is_deleted, is_registration_required, public_code, tenant_id, title, visibility_type_id)
            VALUES
                ({EventId}, {ActorId}, {Guid.Parse("019f6d35-a000-7000-8000-000000000009")},
                 {createdAt}, 1, 1, false, true, 'CONSENT1', {TenantId}, 'Consent migration event', 1);
            INSERT INTO event_reports
                (id, concurrency_stamp, created_at, event_id, priority, reason_code,
                 reporter_contact_consent, reporter_kind, source_kind, status, tenant_id)
            VALUES
                ({LegacyTrueReportId}, {Guid.Parse("019f6d35-a000-7000-8000-00000000000a")},
                 {createdAt}, {EventId}, 1, 'LEGACY_TRUE', true, 1, 1, 1, {TenantId}),
                ({LegacyFalseReportId}, {Guid.Parse("019f6d35-a000-7000-8000-00000000000b")},
                 {createdAt}, {EventId}, 1, 'LEGACY_FALSE', false, 1, 1, 1, {TenantId});
            """);
    }

    private async Task AssertConsentAsync(Guid reportId, bool caseUpdates, bool followUp)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT report_case_updates_consent, report_follow_up_contact_consent
            FROM event_reports
            WHERE id = @reportId
            """, connection);
        command.Parameters.AddWithValue("reportId", reportId);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        await Assert.That(reader.GetBoolean(0)).IsEqualTo(caseUpdates);
        await Assert.That(reader.GetBoolean(1)).IsEqualTo(followUp);
    }

    private async Task AssertLegacyConsentAsync(Guid reportId, bool expected)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT reporter_contact_consent
            FROM event_reports
            WHERE id = @reportId
            """, connection);
        command.Parameters.AddWithValue("reportId", reportId);
        await Assert.That((bool?)await command.ExecuteScalarAsync()).IsEqualTo(expected);
    }

    private async Task ResetSharedMigrationDatabaseAsync()
    {
        var databaseIdentity = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
        if (!databaseIdentity.Database.StartsWith("recipient_delivery_migration_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Refusing to reset a non-test migration database.");
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DROP SCHEMA public CASCADE; CREATE SCHEMA public;", connection);
        await command.ExecuteNonQueryAsync();
    }
}
