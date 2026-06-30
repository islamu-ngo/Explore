// ABOUTME: PostgreSQL Testcontainers fixture for browser E2E scenario seeding.
// ABOUTME: Mirrors persistence integration reset semantics so critical flows can own their data.

using Explore.Persistence;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public sealed class PostgreSqlContainerFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("explore_e2e_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner? _respawner;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
        await LookupTableSeeder.SeedAsync(context);

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = LookupTables
        });
    }

    public async Task ResetAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public ExploreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new ExploreDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    private static readonly Table[] LookupTables =
    [
        new("__EFMigrationsHistory"),
        new("ai_conversation_statuses"),
        new("ai_message_roles"),
        new("ai_proposed_action_kinds"),
        new("ai_proposed_action_statuses"),
        new("ai_reference_kinds"),
        new("ai_run_statuses"),
        new("actor_types"),
        new("analytics_providers"),
        new("approval_statuses"),
        new("audience_ages"),
        new("audience_genders"),
        new("did_custody_types"),
        new("event_formats"),
        new("event_registration_policies"),
        new("event_session_statuses"),
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
