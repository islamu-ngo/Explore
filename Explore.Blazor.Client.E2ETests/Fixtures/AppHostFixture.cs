// ABOUTME: Aspire AppHost fixture for E2E tests starting the full application stack.
// ABOUTME: Provides the Blazor frontend URL to Playwright tests.

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Explore.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public sealed class AppHostFixture : IAsyncInitializer, IAsyncDisposable
{
    public const string SetupSecret = "integration-setup-secret";
    private const string LocalSvixJwtSecret = "local-dev-svix-jwt-secret-change-me";
    private const string LocalSvixAuthToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJvcmdfMjNyYjhZZEdxTVQwcUl6cGdHd2RYZkhpck11In0.8DdxojyqoHnAeZBEL6M1Tcf5i5hnbAmezaRlPxuBXp8";
    private const string LocalSvixOperationalWebhookSecret = "whsec_bG9jYWwtZGV2LXN2aXgtb3BlcmF0aW9uYWwtc2VjcmV0";

    private readonly PostgreSqlContainerFixture _database = new();
    private readonly BffKeycloakFixture _keycloak = new();
    private readonly MailpitContainerFixture _mailpit = new();
    private DistributedApplication? _app;

    public string BlazorBaseUrl => _app?.GetEndpoint("explore-blazor", "http")?.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("Blazor app not started");

    public string ApiBaseUrl => _app?.GetEndpoint("explore-api", "http")?.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("API app not started");

    public string ControlPlaneBaseUrl => _app?.GetEndpoint("event-control-plane", "http")?.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("Control-plane app not started");

    public string KeycloakBaseUrl => _keycloak.BaseUrl;

    public Task ResetDatabaseAsync() => _database.ResetAsync();

    public ExploreDbContext CreateDbContext() => _database.CreateDbContext();

    public Task ClearMailpitMessagesAsync(CancellationToken cancellationToken = default)
        => _mailpit.ClearMessagesAsync(cancellationToken);

    public Task<IReadOnlyList<MailpitContainerFixture.MailpitMessageSummary>> GetMailpitMessagesAsync(
        CancellationToken cancellationToken = default)
        => _mailpit.GetMessagesAsync(cancellationToken);

    public Task<string> GetMailpitMessageTextAsync(string id, CancellationToken cancellationToken = default)
        => _mailpit.GetMessageTextAsync(id, cancellationToken);

    public Task<string> GetMailpitMessageHtmlAsync(string id, CancellationToken cancellationToken = default)
        => _mailpit.GetMessageHtmlAsync(id, cancellationToken);

    public Task<IReadOnlyDictionary<string, string[]>> GetMailpitMessageHeadersAsync(
        string id,
        CancellationToken cancellationToken = default)
        => _mailpit.GetMessageHeadersAsync(id, cancellationToken);

    public Task<string> GetTestUserAccessTokenAsync(CancellationToken cancellationToken = default)
        => _keycloak.GetTestUserAccessTokenAsync(cancellationToken);

    public Task<BffKeycloakFixture.TokenSet> GetTestAdminTokensAsync(CancellationToken cancellationToken = default)
        => _keycloak.GetTestAdminTokensAsync(cancellationToken);

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        await _mailpit.InitializeAsync();
        await PreconfigureTenantRoutingAsync();
        await PreconfigureMailpitSmtpAsync();
        await _keycloak.InitializeAsync();

        var previousAspireMode = Environment.GetEnvironmentVariable("ISLAMU_ASPIRE_MODE");
        Environment.SetEnvironmentVariable("ISLAMU_ASPIRE_MODE", "ExternalInfra");

        IDistributedApplicationTestingBuilder builder;
        try
        {
            builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Explore_AppHost>();

            builder.Services.ConfigureHttpClientDefaults(c =>
                c.ConfigureHttpClient(h => h.BaseAddress = null));

            ConfigureDatabase(builder, _database.ConnectionString);
            var cache = ConfigureCache(builder);
            ConfigureSvix(builder, cache);
            ConfigureKeycloak(builder, _keycloak);
            ConfigureSetupSecret(builder);
            ConfigureEmailDispatch(builder);
            ConfigureDiagnostics(builder);
            ConfigureProjectHttpEndpoint(builder, "explore-api");
            ConfigureProjectHttpEndpoint(builder, "explore-blazor");
            ConfigureProjectHttpEndpoint(builder, "event-control-plane");
            ConfigureApiEndpoint(builder);

            _app = await builder.BuildAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ISLAMU_ASPIRE_MODE", previousAspireMode);
        }
        var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();

        await _app.StartAsync();

        using var apiHealthTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await resourceNotificationService.WaitForResourceHealthyAsync(
            "explore-api",
            apiHealthTimeout.Token);

        using var blazorHealthTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await resourceNotificationService.WaitForResourceHealthyAsync(
            "explore-blazor",
            blazorHealthTimeout.Token);

        using var controlPlaneHealthTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await resourceNotificationService.WaitForResourceHealthyAsync(
            "event-control-plane",
            controlPlaneHealthTimeout.Token);
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
        await _mailpit.DisposeAsync();
        await _database.DisposeAsync();
    }

    private static void ConfigureEmailDispatch(IDistributedApplicationTestingBuilder builder)
    {
        var api = builder.CreateResourceBuilder<ProjectResource>("explore-api");

        api.WithEnvironment("EmailDispatchProcessor__PollingIntervalSeconds", "1");
        api.WithEnvironment("EmailDispatchProcessor__InitialRetryDelaySeconds", "1");
        api.WithEnvironment("EmailDispatchProcessor__BatchSize", "25");
    }

    private static void ConfigureDiagnostics(IDistributedApplicationTestingBuilder builder)
    {
        var api = builder.CreateResourceBuilder<ProjectResource>("explore-api");

        api.WithEnvironment("Diagnostics__EnableAdminCacheInvalidation", "true");
    }

    private static void ConfigureKeycloak(
        IDistributedApplicationTestingBuilder builder,
        BffKeycloakFixture keycloak)
    {
        ConfigureProjectKeycloak(builder, "explore-blazor", keycloak, includeClientCredentials: true);
        ConfigureProjectKeycloak(builder, "explore-api", keycloak, includeClientCredentials: false);
        ConfigureControlPlaneKeycloak(builder, keycloak);
    }

    private static void ConfigureControlPlaneKeycloak(
        IDistributedApplicationTestingBuilder builder,
        BffKeycloakFixture keycloak)
    {
        var resource = builder.CreateResourceBuilder<ProjectResource>("event-control-plane");

        resource.WithEnvironment("Bff__Authentication__Authority", keycloak.Authority);
        resource.WithEnvironment("Bff__Authentication__MetadataAddress", keycloak.MetadataAddress);
        resource.WithEnvironment("Bff__Authentication__ClientId", BffKeycloakFixture.TestControlPlaneClientId);
        resource.WithEnvironment("Bff__Authentication__ClientSecret", BffKeycloakFixture.TestControlPlaneClientSecret);
        resource.WithEnvironment("Bff__Authentication__RequireHttpsMetadata", "false");
        resource.WithEnvironment("KEYCLOAK_REALM", BffKeycloakFixture.RealmName);
        resource.WithEnvironment("KEYCLOAK_ENDPOINT", keycloak.BaseUrl + "/auth");
        resource.WithEnvironment("KEYCLOAK_CONTROL_PLANE_CLIENT_ID", BffKeycloakFixture.TestControlPlaneClientId);
        resource.WithEnvironment("KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET", BffKeycloakFixture.TestControlPlaneClientSecret);
        resource.WithEnvironment("Infisical__ProjectId", string.Empty);
        resource.WithEnvironment("Infisical__ClientId", string.Empty);
        resource.WithEnvironment("Infisical__ClientSecret", string.Empty);
        resource.WithEnvironment("SETUP_SECRET", SetupSecret);
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
        resource.WithEnvironment("SETUP_SECRET", SetupSecret);

        if (includeClientCredentials)
        {
            resource.WithEnvironment("API_ENDPOINT", string.Empty);
            resource.WithEnvironment("ExploreApi__BaseUrl", string.Empty);
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
            resource.WithEnvironment("Infisical__ProjectId", string.Empty);
            resource.WithEnvironment("Infisical__ClientId", string.Empty);
            resource.WithEnvironment("Infisical__ClientSecret", string.Empty);
            resource.WithEnvironment("Keycloak__ValidAudiences__0", "islamu-event-api");
            resource.WithEnvironment("Keycloak__ValidAudiences__1", "islamu-event-blazor");
            resource.WithEnvironment("KeycloakBootstrap__AllowLocalUrls", "true");
        }
    }

    private static void ConfigureSetupSecret(IDistributedApplicationTestingBuilder builder)
    {
        var api = builder.CreateResourceBuilder<ProjectResource>("explore-api");
        var blazor = builder.CreateResourceBuilder<ProjectResource>("explore-blazor");
        var controlPlane = builder.CreateResourceBuilder<ProjectResource>("event-control-plane");

        api.WithEnvironment("SETUP_SECRET", SetupSecret);
        blazor.WithEnvironment("SETUP_SECRET", SetupSecret);
        controlPlane.WithEnvironment("SETUP_SECRET", SetupSecret);
    }

    private static void ConfigureDatabase(
        IDistributedApplicationTestingBuilder builder,
        string connectionString)
    {
        ConfigureProjectDatabase(builder, "explore-api", connectionString);
        ConfigureProjectDatabase(builder, "explore-blazor", connectionString);
    }

    private static void ConfigureProjectDatabase(
        IDistributedApplicationTestingBuilder builder,
        string resourceName,
        string connectionString)
    {
        var resource = builder.CreateResourceBuilder<ProjectResource>(resourceName);

        resource.WithEnvironment("ConnectionStrings__DefaultConnection", connectionString);
        resource.WithEnvironment("Testing__DisableDeploymentModeCache", "true");
        resource.WithEnvironment("Testing__DisableDevelopmentDataSeed", "true");
    }

    private static IResourceBuilder<RedisResource> ConfigureCache(IDistributedApplicationTestingBuilder builder)
    {
        var cache = builder.AddRedis("cache")
            .WithLifetime(ContainerLifetime.Session);

        builder.CreateResourceBuilder<ProjectResource>("explore-api")
            .WithReference(cache)
            .WaitFor(cache);

        builder.CreateResourceBuilder<ProjectResource>("explore-blazor")
            .WithReference(cache)
            .WaitFor(cache);

        return cache;
    }

    private static void ConfigureSvix(
        IDistributedApplicationTestingBuilder builder,
        IResourceBuilder<RedisResource> cache)
    {
        var svixDb = builder.AddContainer("svix-postgres", "postgres", "13.4")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_DB", "postgres")
            .WithLifetime(ContainerLifetime.Session);

        var svix = builder.AddContainer("svix", "svix/svix-server", "latest")
            .WithEnvironment("WAIT_FOR", "true")
            .WithEnvironment("SVIX_DB_DSN", "postgresql://postgres:postgres@svix-postgres:5432/postgres")
            .WithEnvironment("SVIX_QUEUE_TYPE", "redis")
            .WithEnvironment("SVIX_REDIS_DSN", ReferenceExpression.Create($"redis://:{cache.Resource.PasswordParameter!}@cache:6380"))
            .WithEnvironment("SVIX_JWT_SECRET", LocalSvixJwtSecret)
            .WithHttpEndpoint(targetPort: 8071, name: "http")
            .WaitFor(cache)
            .WaitFor(svixDb);

        var api = builder.CreateResourceBuilder<ProjectResource>("explore-api");
        api.WithEnvironment("Webhooks__Provider", "Composite");
        api.WithEnvironment("Webhooks__Svix__BaseUrl", svix.GetEndpoint("http"));
        api.WithEnvironment("Webhooks__Svix__AuthTokenSecretRef", SecretDefinitionRegistry.Keys.Webhooks.SvixAuthToken);
        api.WithEnvironment("Webhooks__Svix__OperationalWebhookSecretRef", SecretDefinitionRegistry.Keys.Webhooks.SvixOperationalWebhookSecret);
        api.WithEnvironment("WEBHOOKS_SVIX_AUTH_TOKEN", LocalSvixAuthToken);
        api.WithEnvironment("WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET", LocalSvixOperationalWebhookSecret);
        api.WaitFor(svix);
    }

    private static void ConfigureProjectHttpEndpoint(
        IDistributedApplicationTestingBuilder builder,
        string resourceName)
    {
        var resource = builder.CreateResourceBuilder<ProjectResource>(resourceName);
        var port = AllocateAvailableTcpPort();

        resource.WithHttpEndpoint(port: port, name: "http");
        if (resourceName == "explore-api")
        {
            resource.WithEnvironment("PublicBaseUrl", "http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static int AllocateAvailableTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static void ConfigureApiEndpoint(IDistributedApplicationTestingBuilder builder)
    {
        var api = builder.CreateResourceBuilder<ProjectResource>("explore-api");
        var blazor = builder.CreateResourceBuilder<ProjectResource>("explore-blazor");
        var controlPlane = builder.CreateResourceBuilder<ProjectResource>("event-control-plane");
        var apiEndpoint = api.GetEndpoint("http");

        blazor.WithEnvironment("API_ENDPOINT", apiEndpoint);
        blazor.WithEnvironment("ExploreApi__BaseUrl", apiEndpoint);
        controlPlane.WithEnvironment("API_ENDPOINT", apiEndpoint);
        controlPlane.WithEnvironment("ExploreApi__BaseUrl", apiEndpoint);
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

    private async Task PreconfigureMailpitSmtpAsync()
    {
        await using var context = _database.CreateDbContext();

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Email.SmtpHost,
            $"\"{_mailpit.SmtpHost}\"",
            SettingValueType.String,
            "Email");

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Email.SmtpPort,
            _mailpit.SmtpPort.ToString(CultureInfo.InvariantCulture),
            SettingValueType.Integer,
            "Email");

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Email.SmtpSecurity,
            "\"None\"",
            SettingValueType.String,
            "Email");

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Email.FromAddress,
            "\"noreply@registration-e2e.test\"",
            SettingValueType.String,
            "Email");

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Email.FromName,
            "\"ISLAMU Event E2E\"",
            SettingValueType.String,
            "Email");

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Email.SmtpTimeoutSeconds,
            "10",
            SettingValueType.Integer,
            "Email");

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
