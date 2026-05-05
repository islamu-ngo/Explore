// ABOUTME: Aspire AppHost fixture for E2E tests starting the full application stack.
// ABOUTME: Provides the Blazor frontend URL to Playwright tests.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public sealed class AppHostFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainerFixture _database = new();
    private readonly BffKeycloakFixture _keycloak = new();
    private DistributedApplication? _app;

    public string BlazorBaseUrl => _app?.GetEndpoint("explore-blazor", "https")?.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("Blazor app not started");

    public string ApiBaseUrl => _app?.GetEndpoint("explore-api", "https")?.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("API app not started");

    public Task ResetDatabaseAsync() => _database.ResetAsync();

    public ExploreDbContext CreateDbContext() => _database.CreateDbContext();

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        await PreconfigureTenantRoutingAsync();
        await _keycloak.InitializeAsync();

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Explore_AppHost>();

        builder.Services.ConfigureHttpClientDefaults(c =>
            c.ConfigureHttpClient(h => h.BaseAddress = null));

        ConfigureDatabase(builder, _database.ConnectionString);
        ConfigureKeycloak(builder, _keycloak);

        _app = await builder.BuildAsync();
        var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();

        await _app.StartAsync();

        await resourceNotificationService.WaitForResourceHealthyAsync(
            "explore-api",
            new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token);

        await resourceNotificationService.WaitForResourceHealthyAsync(
            "explore-blazor",
            new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await _app.StopAsync(stopTimeout.Token);
            await _app.DisposeAsync();
        }

        await _keycloak.DisposeAsync();
        await _database.DisposeAsync();
    }

    private static void ConfigureKeycloak(
        IDistributedApplicationTestingBuilder builder,
        BffKeycloakFixture keycloak)
    {
        ConfigureProjectKeycloak(builder, "explore-blazor", keycloak, includeClientCredentials: true);
        ConfigureProjectKeycloak(builder, "explore-api", keycloak, includeClientCredentials: false);
    }

    private static void ConfigureProjectKeycloak(
        IDistributedApplicationTestingBuilder builder,
        string resourceName,
        BffKeycloakFixture keycloak,
        bool includeClientCredentials)
    {
        var resource = builder.CreateResourceBuilder<ProjectResource>(resourceName);

        resource.WithEnvironment("Keycloak__Authority", keycloak.Authority);
        resource.WithEnvironment("Keycloak__MetadataAddress", keycloak.MetadataAddress);
        resource.WithEnvironment("Keycloak__Realm", BffKeycloakFixture.RealmName);
        resource.WithEnvironment("Keycloak__RequireHttpsMetadata", "false");

        if (includeClientCredentials)
        {
            resource.WithEnvironment("Infisical__ProjectId", string.Empty);
            resource.WithEnvironment("Infisical__ClientId", string.Empty);
            resource.WithEnvironment("Infisical__ClientSecret", string.Empty);
            resource.WithEnvironment("Keycloak__ClientId", BffKeycloakFixture.TestClientId);
            resource.WithEnvironment("Keycloak__ClientSecret", BffKeycloakFixture.TestClientSecret);
            resource.WithEnvironment("KEYCLOAK_CLIENT_ID", BffKeycloakFixture.TestClientId);
            resource.WithEnvironment("KEYCLOAK_BLAZOR_CLIENT_SECRET", BffKeycloakFixture.TestClientSecret);
        }
        else
        {
            resource.WithEnvironment("Keycloak__ValidAudiences__0", "islamu-event-api");
            resource.WithEnvironment("Keycloak__ValidAudiences__1", "islamu-event-blazor");
        }
    }

    private static void ConfigureDatabase(
        IDistributedApplicationTestingBuilder builder,
        string connectionString)
    {
        ConfigureProjectDatabase(
            builder,
            "event-migrationservice",
            connectionString,
            includeMigrationServiceConnectionString: true);

        ConfigureProjectDatabase(builder, "explore-api", connectionString);
        ConfigureProjectDatabase(builder, "explore-blazor", connectionString);
    }

    private static void ConfigureProjectDatabase(
        IDistributedApplicationTestingBuilder builder,
        string resourceName,
        string connectionString,
        bool includeMigrationServiceConnectionString = false)
    {
        var resource = builder.CreateResourceBuilder<ProjectResource>(resourceName);

        resource.WithEnvironment("ConnectionStrings__DefaultConnection", connectionString);

        if (includeMigrationServiceConnectionString)
        {
            resource.WithEnvironment("ConnectionStrings__EventMigrationService", connectionString);
        }
    }

    private async Task PreconfigureTenantRoutingAsync()
    {
        await using var context = _database.CreateDbContext();

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Deployment.Mode,
            $"\"{DeploymentMode.MultiTenant}\"",
            SettingValueType.String,
            "System");

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Routing.ResolverPathEnabled,
            "true",
            SettingValueType.Boolean,
            "Routing");

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Routing.PathPrefix,
            "\"/t\"",
            SettingValueType.String,
            "Routing");

        await context.SaveChangesAsync();
    }

    private static void UpsertSystemSetting(
        ExploreDbContext context,
        string settingKey,
        string value,
        SettingValueType valueType,
        string category)
    {
        var setting = context.SystemSettings.Local.FirstOrDefault(x => x.SettingKey == settingKey)
            ?? context.SystemSettings.FirstOrDefault(x => x.SettingKey == settingKey);

        if (setting is null)
        {
            context.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                SettingKey = settingKey,
                Value = value,
                ValueType = valueType,
                Category = category,
                CreatedAt = DateTime.UtcNow
            });

            return;
        }

        setting.Value = value;
        setting.ValueType = valueType;
        setting.Category ??= category;
        setting.UpdatedAt = DateTime.UtcNow;
    }
}
