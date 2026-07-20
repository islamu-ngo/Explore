// ABOUTME: Verifies hardened AT Protocol federation persistence in the rebased PostgreSQL baseline.
// ABOUTME: Covers the final tables, constraints, and source-version uniqueness without deleted history boundaries.

using Event.Persistence.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class AtprotoFederationBaselineGuardTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task CurrentBaseline_ContainsHardenedFederationSchemaAndGuards()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();

        await Assert.That(await context.Database.GetAppliedMigrationsAsync())
            .Contains("20260719221539_init");
        await Assert.That(await ReadCountAsync(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' " +
            "AND table_name IN ('atproto_records', 'pds_sync_outbox', 'atproto_jetstream_consumer_states', " +
            "'atproto_jetstream_quarantines', 'atproto_record_tenant_presentations', 'atproto_outbound_record_ownerships')"))
            .IsEqualTo(6L);
        await Assert.That(await ReadCountAsync(
            "SELECT COUNT(*) FROM pg_constraint WHERE conname IN " +
            "('ck_atproto_records_direction', 'ck_atproto_records_provenance', " +
            "'ck_pds_sync_outbox_operation', 'ck_pds_sync_outbox_status', 'ck_pds_sync_outbox_payload_shape')"))
            .IsEqualTo(5L);
        await Assert.That(await ReadCountAsync(
            "SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public' " +
            "AND indexname IN ('ux_atproto_records_identity', 'ux_pds_sync_outbox_source_version')"))
            .IsEqualTo(2L);
    }

    private async Task<long> ReadCountAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
