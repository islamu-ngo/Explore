// ABOUTME: PostgreSQL RLS prototype tests for tenant session variables on EF Core connection open.
// ABOUTME: Proves forced RLS policies honor app.current_tenant_id across pooled EF Core/Npgsql connections.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Persistence;
using Explore.Persistence.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.TenantIsolation;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public class PostgresTenantSessionRlsPrototypeTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task ConnectionInterceptor_ShouldBindCurrentTenantForForcedRlsPolicies()
    {
        var tableName = $"rls_tenant_session_probe_{Guid.NewGuid():N}";
        var roleName = $"rls_tenant_session_reader_{Guid.NewGuid():N}";
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await CreateForcedRlsProbeTableAsync(tableName, roleName, tenantA, tenantB);

        try
        {
            var dbContextFactory = CreateDbContextFactory();

            var tenantAResult = await ReadVisibleProbeRowsAsync(dbContextFactory, tenantA, tableName, roleName);
            var tenantBResult = await ReadVisibleProbeRowsAsync(dbContextFactory, tenantB, tableName, roleName);
            var noTenantResult = await ReadVisibleProbeRowsAsync(dbContextFactory, null, tableName, roleName);

            await Assert.That(tenantAResult.SessionTenantId).IsEqualTo(tenantA.ToString());
            await Assert.That(tenantAResult.Names).IsEquivalentTo(["tenant-a"]);
            await Assert.That(tenantBResult.SessionTenantId).IsEqualTo(tenantB.ToString());
            await Assert.That(tenantBResult.Names).IsEquivalentTo(["tenant-b"]);
            await Assert.That(noTenantResult.SessionTenantId).IsEqualTo(string.Empty);
            await Assert.That(noTenantResult.Names).IsEmpty();
        }
        finally
        {
            await DropProbeTableAsync(tableName, roleName);
        }
    }

    private PooledDbContextFactory<ExploreDbContext> CreateDbContextFactory()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(PostgresTenantSessionInterceptor.Instance)
            .Options;

        return new PooledDbContextFactory<ExploreDbContext>(options, poolSize: 1);
    }

    private async Task CreateForcedRlsProbeTableAsync(string tableName, string roleName, Guid tenantA, Guid tenantB)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        var tableIdentifier = QuoteIdentifier(tableName);
        var roleIdentifier = QuoteIdentifier(roleName);
        var policyIdentifier = QuoteIdentifier($"{tableName}_tenant_isolation");

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            create role {roleIdentifier};

            create table public.{tableIdentifier}
            (
                id uuid primary key,
                tenant_id uuid not null,
                name text not null
            );

            insert into public.{tableIdentifier} (id, tenant_id, name)
            values
                (@tenant_a_row_id, @tenant_a_id, @tenant_a_name),
                (@tenant_b_row_id, @tenant_b_id, @tenant_b_name);

            alter table public.{tableIdentifier} enable row level security;
            alter table public.{tableIdentifier} force row level security;

            create policy {policyIdentifier}
                on public.{tableIdentifier}
                for all
                using (tenant_id = nullif(current_setting('{PostgresTenantSessionInterceptor.CurrentTenantSettingName}', true), '')::uuid)
                with check (tenant_id = nullif(current_setting('{PostgresTenantSessionInterceptor.CurrentTenantSettingName}', true), '')::uuid);

            grant usage on schema public to {roleIdentifier};
            grant select on public.{tableIdentifier} to {roleIdentifier};
            """;
        command.Parameters.AddWithValue("tenant_a_row_id", Guid.NewGuid());
        command.Parameters.AddWithValue("tenant_a_id", tenantA);
        command.Parameters.AddWithValue("tenant_a_name", "tenant-a");
        command.Parameters.AddWithValue("tenant_b_row_id", Guid.NewGuid());
        command.Parameters.AddWithValue("tenant_b_id", tenantB);
        command.Parameters.AddWithValue("tenant_b_name", "tenant-b");

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<ProbeReadResult> ReadVisibleProbeRowsAsync(
        IDbContextFactory<ExploreDbContext> dbContextFactory,
        Guid? tenantId,
        string tableName,
        string roleName)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.TenantContext = tenantId.HasValue ? new TestTenantContext(tenantId.Value) : null;

        return await ReadVisibleProbeRowsAsync(context, tableName, roleName);
    }

    private static async Task<ProbeReadResult> ReadVisibleProbeRowsAsync(ExploreDbContext context, string tableName, string roleName)
    {
        await context.Database.OpenConnectionAsync();

        try
        {
            var connection = context.Database.GetDbConnection();
            using var setRoleCommand = connection.CreateCommand();
            setRoleCommand.CommandText = $"set role {QuoteIdentifier(roleName)}";
            await setRoleCommand.ExecuteNonQueryAsync();

            try
            {
                using var settingCommand = connection.CreateCommand();
                settingCommand.CommandText = $"select current_setting('{PostgresTenantSessionInterceptor.CurrentTenantSettingName}', true)";
                var sessionTenantId = (string?)await settingCommand.ExecuteScalarAsync();

                using var rowsCommand = connection.CreateCommand();
                rowsCommand.CommandText = $"select name from public.{QuoteIdentifier(tableName)} order by name";

                var names = new List<string>();
                await using var reader = await rowsCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    names.Add(reader.GetString(0));
                }

                return new ProbeReadResult(sessionTenantId, names);
            }
            finally
            {
                using var resetRoleCommand = connection.CreateCommand();
                resetRoleCommand.CommandText = "reset role";
                await resetRoleCommand.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private async Task DropProbeTableAsync(string tableName, string roleName)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            drop table if exists public.{QuoteIdentifier(tableName)};
            revoke usage on schema public from {QuoteIdentifier(roleName)};
            drop role if exists {QuoteIdentifier(roleName)};
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;

    private sealed record ProbeReadResult(string? SessionTenantId, IReadOnlyList<string> Names);
}
