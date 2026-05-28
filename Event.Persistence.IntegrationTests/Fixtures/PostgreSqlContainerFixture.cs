// ABOUTME: PostgreSQL container fixture for persistence integration tests using Testcontainers.
// ABOUTME: Provides container lifecycle, schema migration via MigrateAsync, lookup seeding, and Respawn-based reset.

using Explore.Application.Contracts.Infrastructure;
using Explore.Persistence;
using Explore.Persistence.Schema;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Event.Persistence.IntegrationTests.Fixtures;

/// <summary>
/// Shared PostgreSQL container fixture that provides production-faithful schema via MigrateAsync
/// and deterministic state reset via Respawn between tests.
/// </summary>
public class PostgreSqlContainerFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;
    private Respawner? _respawner;

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("explore_db_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    /// <summary>
    /// Connection string for the running PostgreSQL container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply all migrations for production-faithful schema
        await using var context = CreateDbContextInternal();
        await context.Database.MigrateAsync();
        await PostgresModelConstraintApplier.ApplyAsync(context);
        await LookupTableSeeder.SeedAsync(context);

        // Initialize Respawn for deterministic reset between tests
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = LookupTables
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Creates a fresh DbContext connected to the test container.
    /// Schema and lookup data are already present from initialization.
    /// </summary>
    public ExploreDbContext CreateDbContext()
    {
        var context = CreateDbContextInternal();
        context.EnableTenantFilterBypass("Persistence integration test system context.");
        return context;
    }

    /// <summary>
    /// Creates a DbContext with tenant filters enforced. Use for tenant-isolation tests.
    /// </summary>
    public ExploreDbContext CreateTenantFilteredDbContext(ITenantContext? tenantContext = null)
    {
        var context = CreateDbContextInternal();
        context.TenantContext = tenantContext;
        return context;
    }

    /// <summary>
    /// Resets the database to a clean state, preserving schema and lookup data.
    /// Call at the start of tests that need deterministic state.
    /// </summary>
    public async Task ResetAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    private ExploreDbContext CreateDbContextInternal()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            // Pending-model-changes drift is tolerated in integration tests because concurrent
            // developer branches may add model edits ahead of a consolidated migration.
            // Tests exercise real SQL against the schema that migrations do produce.
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new ExploreDbContext(options);
    }

    /// <summary>
    /// Lookup tables seeded by LookupTableSeeder that Respawn must preserve.
    /// </summary>
    private static readonly Table[] LookupTables =
    [
        new("__EFMigrationsHistory"),
        new("actor_types"),
        new("analytics_providers"),
        new("approval_statuses"),
        new("audience_ages"),
        new("audience_genders"),
        new("did_custody_types"),
        new("event_formats"),
        new("event_registration_policies"),
        new("event_statuses"),
        new("event_types"),
        new("external_api_key_credit_periods"),
        new("external_api_key_owner_types"),
        new("external_api_key_statuses"),
        new("file_types"),
        new("group_positions"),
        new("languages"),
        new("madhabs"),
        new("module_definitions"),
        new("notification_entity_types"),
        new("notification_scope_types"),
        new("notification_types"),
        new("organization_positions"),
        new("permissions"),
        new("registration_modes"),
        new("registration_scopes"),
        new("role_scopes"),
        new("roles"),
        new("schedule_item_kinds"),
        new("secret_source_types"),
        new("secret_validation_statuses"),
        new("setting_scopes"),
        new("setting_value_types"),
        new("system_settings"),
        new("tag_types"),
        new("tenant_footer_link_groups"),
        new("tenant_footer_links"),
        new("tenant_statuses"),
        new("visibility_types"),
    ];
}
