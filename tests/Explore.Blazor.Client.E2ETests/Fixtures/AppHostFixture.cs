// ABOUTME: Aspire AppHost fixture for E2E tests starting the full application stack.
// ABOUTME: Provides application endpoints, authenticated API clients, and test-only runtime configuration seams.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public sealed class AppHostFixture : IAsyncInitializer, IAsyncDisposable
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);
    public const string SetupSecret = "integration-setup-secret";
    private const string LocalSvixJwtSecret = "local-dev-svix-jwt-secret-change-me";
    private const string LocalSvixAuthToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJvcmdfMjNyYjhZZEdxTVQwcUl6cGdHd2RYZkhpck11In0.8DdxojyqoHnAeZBEL6M1Tcf5i5hnbAmezaRlPxuBXp8";
    private const string LocalSvixOperationalWebhookSecret = "whsec_bG9jYWwtZGV2LXN2aXgtb3BlcmF0aW9uYWwtc2VjcmV0";
    private const string SvixAuthTokenSecretRef = "webhooks.svix.auth_token";
    private const string SvixOperationalWebhookSecretRef = "webhooks.svix.operational_webhook_secret";

    private readonly PostgreSqlContainerFixture _database = new();
    private readonly BffKeycloakFixture _keycloak = new();
    private readonly MailpitContainerFixture _mailpit = new();
    private readonly List<HttpClient> _apiHttpClients = [];
    private Task<BffKeycloakFixture.TokenSet>? _testAdminTokens;
    private DistributedApplication? _app;

    public string BlazorBaseUrl => _app?.GetEndpoint("explore-blazor", "http")?.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("Blazor app not started");

    public string ApiBaseUrl => _app?.GetEndpoint("explore-api", "http")?.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("API app not started");

    public string KeycloakBaseUrl => _keycloak.BaseUrl;

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
        => _testAdminTokens ??= _keycloak.GetTestAdminTokensAsync(cancellationToken);

    public async Task EnableSupportAccessAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_database.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO system_settings
                (setting_key, value, setting_value_type_id, is_locked, category, description)
            VALUES
                ('support_access.enabled', 'true', 2, true, 'SupportAccess', 'Support-access E2E setting.')
            ON CONFLICT (setting_key) DO UPDATE SET
                value = EXCLUDED.value,
                setting_value_type_id = EXCLUDED.setting_value_type_id,
                is_locked = EXCLUDED.is_locked,
                updated_at = NOW();
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public IEventApiClient CreateApiClient(
        string accessToken,
        string? tenantSlug = null,
        bool includeSetupSecret = false)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl + "/", UriKind.Absolute) };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (!string.IsNullOrWhiteSpace(tenantSlug))
        {
            httpClient.DefaultRequestHeaders.Add("X-Tenant-Slug", tenantSlug);
        }

        if (includeSetupSecret)
        {
            httpClient.DefaultRequestHeaders.Add("X-Setup-Secret", SetupSecret);
        }

        _apiHttpClients.Add(httpClient);
        return new EventApiClient(httpClient);
    }

    public async Task<ApiAdminIdentitySnapshot> SnapshotApiAdminIdentityAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl + "/", UriKind.Absolute) };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.PostAsync(
            "api/_internal/admin-cache/current-user/snapshot",
            content: null,
            cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"API admin identity snapshot failed with {(int)response.StatusCode}: {content}");
        }

        return JsonSerializer.Deserialize<ApiAdminIdentitySnapshot>(
                content,
                WebJsonOptions)
            ?? throw new InvalidOperationException("API admin identity snapshot returned an empty response.");
    }

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        await _mailpit.InitializeAsync();
        await _keycloak.InitializeAsync();

        var previousAspireMode = Environment.GetEnvironmentVariable("ISLAMU_ASPIRE_MODE");
        Environment.SetEnvironmentVariable("ISLAMU_ASPIRE_MODE", "ExternalInfra");

        IDistributedApplicationTestingBuilder builder;
        try
        {
            builder = await DistributedApplicationTestingBuilder.CreateAsync(LoadAppHostEntryPoint());

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

        await BootstrapInstanceAsync();

        using var blazorHealthTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await resourceNotificationService.WaitForResourceHealthyAsync(
            "explore-blazor",
            blazorHealthTimeout.Token);

    }

    public async ValueTask DisposeAsync()
    {
        foreach (var httpClient in _apiHttpClients)
        {
            httpClient.Dispose();
        }

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
            resource.WithEnvironment("Keycloak__Audience", "islamu-event-api");
            resource.WithEnvironment("Keycloak__ValidAudiences__0", "islamu-event-api");
            resource.WithEnvironment("Keycloak__ValidAudiences__1", "islamu-event-blazor");
            resource.WithEnvironment("KeycloakBootstrap__AllowLocalUrls", "true");
        }
    }

    private static void ConfigureSetupSecret(IDistributedApplicationTestingBuilder builder)
    {
        var api = builder.CreateResourceBuilder<ProjectResource>("explore-api");
        var blazor = builder.CreateResourceBuilder<ProjectResource>("explore-blazor");

        api.WithEnvironment("SETUP_SECRET", SetupSecret);
        blazor.WithEnvironment("SETUP_SECRET", SetupSecret);
    }

    private static void ConfigureDatabase(
        IDistributedApplicationTestingBuilder builder,
        string connectionString)
    {
        ConfigureProjectDatabase(builder, "event-migrationservice", connectionString);
        ConfigureProjectDatabase(builder, "explore-api", connectionString);
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

        var svix = builder.AddContainer("svix", "svix/svix-server", "v1.96.1")
            .WithEnvironment("WAIT_FOR", "true")
            .WithEnvironment("SVIX_DB_DSN", "postgresql://postgres:postgres@svix-postgres:5432/postgres")
            .WithEnvironment("SVIX_QUEUE_TYPE", "redis")
            .WithEnvironment("SVIX_CACHE_TYPE", "redis")
            .WithEnvironment("SVIX_REDIS_DSN", ReferenceExpression.Create($"redis://:{cache.Resource.PasswordParameter!}@cache:6380"))
            .WithEnvironment("SVIX_JWT_SECRET", LocalSvixJwtSecret)
            .WithHttpEndpoint(targetPort: 8071, name: "http")
            .WaitFor(cache)
            .WaitFor(svixDb);

        var api = builder.CreateResourceBuilder<ProjectResource>("explore-api");
        api.WithEnvironment("Webhooks__Provider", "Composite");
        api.WithEnvironment("Webhooks__Svix__BaseUrl", svix.GetEndpoint("http"));
        api.WithEnvironment("Webhooks__Svix__Environment", "self-hosted");
        api.WithEnvironment("Webhooks__Svix__ProviderVersion", "1.96.1");
        api.WithEnvironment("Webhooks__Svix__CapabilityPolicyVersion", "svix-self-hosted-1.96.1-v1");
        api.WithEnvironment("Webhooks__Svix__AuthTokenSecretRef", SvixAuthTokenSecretRef);
        api.WithEnvironment("Webhooks__Svix__OperationalWebhookSecretRef", SvixOperationalWebhookSecretRef);
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
        var apiEndpoint = api.GetEndpoint("http");

        blazor.WithEnvironment("API_ENDPOINT", apiEndpoint);
        blazor.WithEnvironment("ExploreApi__BaseUrl", apiEndpoint);
    }

    private async Task BootstrapInstanceAsync()
    {
        var tokens = await GetTestAdminTokensAsync();
        var api = CreateApiClient(tokens.AccessToken, includeSetupSecret: true);
        var preflight = await api.GetSystemOnboardingPreflightAsync();
        if (preflight.IsReadyToLaunch != true)
        {
            var blockers = preflight.BlockingChecks?
                .Where(check => !string.Equals(check.Status, "Pass", StringComparison.OrdinalIgnoreCase))
                .Select(check => $"{check.Code}: {check.Message} {check.Detail}".Trim())
                .ToArray() ?? [];
            throw new InvalidOperationException(
                $"E2E instance onboarding preflight failed: {string.Join(" | ", blockers)}");
        }

        var sync = await api.SyncUserAsync();
        EnsureSuccess(sync, "syncing the E2E instance administrator");

        BaseCommandResponseOfGuid onboarding;
        try
        {
            onboarding = await api.CompleteInstanceOnboardingAsync(new CompleteInstanceOnboardingRequest
            {
                DeploymentMode = DeploymentMode.SingleTenant,
                SiteProfile = new SelfHostOnboardingProfileDto
                {
                    SiteName = "ISLAMU Event E2E",
                    Locale = "en",
                    TimeZone = "UTC"
                },
                AdministrationAccessMode = "Embedded",
                InstanceName = "ISLAMU Event E2E"
            });
        }
        catch (ApiException<ValidationProblemDetails> exception)
        {
            var errors = exception.Result.Errors?
                .SelectMany(pair => pair.Value.Select(message => $"{pair.Key}: {message}"))
                .ToArray() ?? [];
            throw new InvalidOperationException(
                "E2E instance onboarding request was rejected. " +
                $"Title={exception.Result.Title}. Detail={exception.Result.Detail}. " +
                $"Errors={string.Join(" | ", errors)}",
                exception);
        }
        EnsureSuccess(onboarding, "completing E2E instance onboarding");

        BaseCommandResponseOfGuid smtp;
        try
        {
            smtp = await api.UpdateInstanceSmtpSettingsAsync(new InstanceSmtpSettingsDto
            {
                Host = _mailpit.SmtpHost,
                Port = _mailpit.SmtpPort,
                Username = string.Empty,
                Password = string.Empty,
                Security = "None",
                FromAddress = "noreply@registration-e2e.test",
                FromName = "ISLAMU Event E2E",
                TimeoutSeconds = 10,
                SkipCertificateValidation = false
            });
        }
        catch (ApiException<ValidationProblemDetails> exception)
        {
            var errors = exception.Result.Errors?
                .SelectMany(pair => pair.Value.Select(message => $"{pair.Key}: {message}"))
                .ToArray() ?? [];
            throw new InvalidOperationException(
                "E2E SMTP configuration request was rejected. " +
                $"Title={exception.Result.Title}. Detail={exception.Result.Detail}. " +
                $"Errors={string.Join(" | ", errors)}",
                exception);
        }
        EnsureSuccess(smtp, "configuring E2E SMTP");
    }

    private static Type LoadAppHostEntryPoint()
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var configuration = IsDebugBuild() ? "Debug" : "Release";
        var assemblyPath = Path.Combine(
            repositoryRoot,
            "src",
            "Explore.AppHost",
            "bin",
            configuration,
            "net10.0",
            "Explore.AppHost.dll");
        var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
        return assembly.EntryPoint?.DeclaringType
            ?? throw new InvalidOperationException($"AppHost entry point was not found in '{assemblyPath}'.");
    }

    private static string FindRepositoryRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Event.slnx")) ||
                Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root for the E2E AppHost.");
    }

    private static bool IsDebugBuild()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }

    private static void EnsureSuccess(BaseCommandResponseOfGuid response, string operation)
    {
        if (response.Success != true)
        {
            throw new InvalidOperationException($"API failed while {operation}: {response.Message}");
        }
    }

    public sealed record ApiAdminIdentitySnapshot(
        string? AuthenticationType,
        string? InternalUserIdClaim,
        string? SubjectClaim,
        string? SessionIdClaim,
        string? NameIdentifierClaim,
        string? Provider,
        string? ProviderId,
        Guid? ResolvedUserId);
}
