// ABOUTME: Probes the ATProto OAuth session protection migration and its fail-closed rollback guards.
// ABOUTME: Verifies legacy/encrypted rows block unsafe transitions and the migration stays single-purpose.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class ProtectAtprotoOAuthSessionsMigrationTests(PostgreSqlContainerFixture fixture)
{
    private const string PreviousMigration = "20260718203920_AddEmailDispatchContentRetention";
    private const string TargetMigration = "20260718205141_ProtectAtprotoOAuthSessions";

    [Test]
    public async Task EmptyAndPopulatedTransitionsFailClosedWithoutLosingSchemaRecoverability()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            await migrator.MigrateAsync(TargetMigration);
            await migrator.MigrateAsync(PreviousMigration);
            var (tenantId, userId) = await SeedScopeAsync(context);
            await InsertLegacySessionAsync(tenantId, userId);

            var legacyFailure = await Assert.That(async () => await migrator.MigrateAsync(TargetMigration))
                .Throws<PostgresException>();
            await Assert.That(legacyFailure!.MessageText).Contains("requires user_authentication_tokens to be empty");
            await ExecuteAsync("DELETE FROM user_authentication_tokens");
            await migrator.MigrateAsync(TargetMigration);
            await InsertEncryptedSessionAsync(tenantId, userId);

            var downgradeFailure = await Assert.That(async () => await migrator.MigrateAsync(PreviousMigration))
                .Throws<PostgresException>();
            await Assert.That(downgradeFailure!.MessageText).Contains("Cannot downgrade ProtectAtprotoOAuthSessions");
            await ExecuteAsync("DELETE FROM user_authentication_tokens");
            await migrator.MigrateAsync(PreviousMigration);
            await migrator.MigrateAsync(TargetMigration);
        }
        finally
        {
            await ExecuteAsync("DELETE FROM user_authentication_tokens");
            await migrator.MigrateAsync();
        }
    }

    private async Task<(Guid TenantId, Guid UserId)> SeedScopeAsync(ExploreDbContext context)
    {
        var activeStatus = await context.TenantStatuses.SingleAsync(status =>
            status.Id == (int)TenantStatusEnum.Active);
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = "ATProto migration",
            Slug = $"atproto-migration-{Guid.NewGuid():N}"[..32],
            TenantStatusId = activeStatus.Id,
            TenantStatus = activeStatus
        };
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"atproto-migration-{Guid.NewGuid():N}@example.test",
                FirstName = "ATProto",
                LastName = "Migration"
            }
        };
        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return (tenant.Id, user.Id);
    }

    private async Task InsertLegacySessionAsync(Guid tenantId, Guid userId)
    {
        const string sql =
            """
            INSERT INTO user_authentication_tokens
                (id, user_id, tenant_id, provider, access_token, refresh_token, dpop_key, id_token, created_at)
            VALUES
                (@id, @user_id, @tenant_id, 'atproto', 'legacy-access', 'legacy-refresh', 'legacy-dpop', 'legacy-id', NOW())
            """;
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task InsertEncryptedSessionAsync(Guid tenantId, Guid userId)
    {
        const string sql =
            """
            INSERT INTO user_authentication_tokens
                (id, user_id, tenant_id, provider, subject_did, session_ciphertext, encryption_key_id,
                 o_auth_client_key_id, envelope_version, concurrency_stamp, pds_host, created_at)
            VALUES
                (@id, @user_id, @tenant_id, 'atproto', 'did:plc:migration', @ciphertext, 'active-key',
                 'oauth-client-key', 1, @stamp, 'https://pds.example/', NOW())
            """;
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("ciphertext", Enumerable.Repeat((byte)1, 29).ToArray());
        command.Parameters.AddWithValue("stamp", Guid.CreateVersion7());
        await command.ExecuteNonQueryAsync();
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

}

public sealed class ProtectAtprotoOAuthSessionsMigrationOperationTests
{
    [Test]
    public async Task OperationsAreRestrictedToTheOAuthSessionTableAndContainBothGuards()
    {
        var migration = new ProbeMigration();
        var up = migration.BuildUp();
        var down = migration.BuildDown();
        var operations = up.Concat(down).ToArray();

        foreach (var operation in operations)
        {
            var table = operation.GetType().GetProperty("Table")?.GetValue(operation) as string;
            await Assert.That(table is null || table == "user_authentication_tokens").IsTrue();
            if (operation is SqlOperation sql)
            {
                await Assert.That(sql.Sql).DoesNotContain("email_dispatch");
            }
        }

        await Assert.That(up.OfType<SqlOperation>().Single().Sql)
            .Contains("legacy plaintext sessions must be revoked");
        await Assert.That(down.OfType<SqlOperation>().Single().Sql)
            .Contains("plaintext credentials cannot be reconstructed");
        await Assert.That(up.OfType<CreateIndexOperation>().Single().Name)
            .IsEqualTo("ux_user_authentication_tokens_tenant_provider_subject_did");
        await Assert.That(up.OfType<AddCheckConstraintOperation>().Select(operation => operation.Name))
            .IsEquivalentTo(new[]
            {
                "ck_user_authentication_tokens_ciphertext_not_empty",
                "ck_user_authentication_tokens_envelope_version",
                "ck_user_authentication_tokens_required_text"
            });
    }

    private sealed class ProbeMigration : ProtectAtprotoOAuthSessions
    {
        public IReadOnlyList<MigrationOperation> BuildUp()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            Up(builder);
            return builder.Operations;
        }

        public IReadOnlyList<MigrationOperation> BuildDown()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            Down(builder);
            return builder.Operations;
        }
    }
}
