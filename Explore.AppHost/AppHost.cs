// ABOUTME: .NET Aspire AppHost for profile-driven local development orchestration.
// ABOUTME: Branches full, core, and lite topologies while keeping app projects unchanged.

using System.Net.Sockets;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

const string LocalCerbosAdminUsername = "cerbos";
const string LocalCerbosAdminPassword = "cerbos";
const string LocalCerbosAdminPasswordHash =
    "JDJiJDEwJGxUWWVjblZpTlRseTZvUkhQS3Y5U2VKZGpwZzdqWkFRcGV2S2Ezbkxpbk55bDF5U1dEZVkyCg==";
const string LocalOspreyImage = "ghcr.io/roostorg/osprey/osprey-coordinator";
const string LocalOspreyTag = "latest";
const string LocalMailpitImage = "axllent/mailpit";
const string LocalMailpitTag = "latest";
const string LocalSvixJwtSecret = "local-dev-svix-jwt-secret-change-me";
const string LocalSvixAuthToken =
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJvcmdfMjNyYjhZZEdxTVQwcUl6cGdHd2RYZkhpck11In0.8DdxojyqoHnAeZBEL6M1Tcf5i5hnbAmezaRlPxuBXp8";
const string LocalSvixOperationalWebhookSecret = "whsec_bG9jYWwtZGV2LXN2aXgtb3BlcmF0aW9uYWwtc2VjcmV0";

var builder = DistributedApplication.CreateBuilder(args);
var runMode = AspireRunModeExtensions.Parse(builder.Configuration["ISLAMU_ASPIRE_MODE"]);
var repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory());
var appHostConfigRoot = Path.Combine(repositoryRoot, "Explore.AppHost", "Config");
var cerbosPolicyPackagePath = Path.Combine(repositoryRoot, "cerbos", "policies");
var cerbosConfigPath = Path.Combine(repositoryRoot, "cerbos", "config", ".cerbos.yaml");
var cerbosSchemaPath = Path.Combine(repositoryRoot, "cerbos", "init", "cerbos-schema.sql");
var keycloakRealmExportPath = Path.Combine(repositoryRoot, "docker", "keycloak", "realm-export.json");
var localStorageRootPath = Path.Combine(repositoryRoot, "storage-data", "aspire-local");
var prometheusConfigPath = Path.Combine(appHostConfigRoot, "prometheus.yaml");
var grafanaDashboardPath = Path.Combine(appHostConfigRoot, "grafana-dashboard");
Directory.CreateDirectory(localStorageRootPath);
Directory.CreateDirectory(grafanaDashboardPath);

Console.WriteLine("===========================================");
Console.WriteLine("Explore AppHost - Local Development Orchestrator");
Console.WriteLine($"Mode: {runMode}");
Console.WriteLine("local-full: local platform; local-core: local data/cache; local-lite: external infrastructure");
Console.WriteLine("===========================================");

// Delayed health check for startup sequencing
var startAfter = DateTime.Now.AddSeconds(30);
builder.Services.AddHealthChecks().AddCheck("startup-delay", () =>
    DateTime.Now > startAfter ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());

IResourceBuilder<PostgresDatabaseResource>? database = null;
IResourceBuilder<RedisResource>? cache = null;
IResourceBuilder<RabbitMQServerResource>? messaging = null;
FullLocalResources? fullLocalResources = null;
var mailpit = AddMailpit(builder);

if (runMode.UsesLocalData())
{
    database = builder.AddPostgres("postgres")
        .WithImageTag("18-alpine")
        .WithDataVolume("islamu-event-postgres-data")
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("explore");

    cache = builder.AddRedis("cache")
        .WithDataVolume("islamu-event-redis-data")
        .WithLifetime(ContainerLifetime.Persistent);
}

if (runMode == AspireRunMode.FullLocal)
{
    messaging = builder.AddRabbitMQ("messaging")
        .WithManagementPlugin()
        .WithDataVolume("islamu-event-rabbitmq-data")
        .WithLifetime(ContainerLifetime.Persistent);

    fullLocalResources = AddFullLocalPlatform(
        builder,
        cerbosConfigPath,
        cerbosPolicyPackagePath,
        cerbosSchemaPath,
        LocalCerbosAdminUsername,
        LocalCerbosAdminPasswordHash,
        keycloakRealmExportPath,
        prometheusConfigPath,
        grafanaDashboardPath,
        cache);
}

IResourceBuilder<ProjectResource>? migrations = null;
if (database is not null)
{
    migrations = WithProfileSecretMode(
            builder.AddProject<Projects.Event_MigrationService>(
                "event-migrationservice",
                ExcludeProjectLaunchProfile),
            runMode,
            builder.Configuration)
        .WithReference(database, connectionName: "EventMigrationService")
        .WithReference(database, connectionName: "DefaultConnection")
        .WaitFor(database);

    migrations = ConfigureLocalMailpitSmtp(migrations);
}

var exploreAPI = WithProfileSecretMode(
        builder.AddProject<Projects.Explore_API>(
                "explore-api",
                ExcludeProjectLaunchProfile)
            .WithHttpEndpoint(name: "http"),
        runMode,
        builder.Configuration)
    .WithEnvironment("HttpsRedirection__Enabled", "false")
    .WithEnvironment("Cerbos__PolicyPackagePath", cerbosPolicyPackagePath)
    .WithEnvironment("Storage__Local__RootPath", localStorageRootPath)
    .WithEnvironment("Storage__Local__CreateRootIfMissing", "true")
    .WithEnvironment("StorageReconciliation__Enabled", "true")
    .WithEnvironment("StorageReconciliation__DryRun", "true")
    .WaitFor(mailpit);

exploreAPI = ConfigureLocalMailpitSmtp(exploreAPI);

if (migrations is not null)
{
    exploreAPI = exploreAPI
        .WithReference(migrations)
        .WaitForCompletion(migrations);
}

if (database is not null)
{
    exploreAPI = exploreAPI
        .WithReference(database, connectionName: "DefaultConnection")
        .WaitFor(database);
}

if (cache is not null)
{
    exploreAPI = exploreAPI
        .WithReference(cache)
        .WaitFor(cache);
}

if (messaging is not null)
{
    exploreAPI = exploreAPI
        .WithReference(messaging)
        .WaitFor(messaging)
        .WithEnvironment("EmailDispatchRabbitMq__Enabled", "true");
}
else
{
    exploreAPI = exploreAPI.WithEnvironment("EmailDispatchRabbitMq__Enabled", "false");
}

if (fullLocalResources is not null)
{
    exploreAPI = ConfigureFullLocalApi(
            exploreAPI,
            fullLocalResources,
            LocalCerbosAdminUsername,
            LocalCerbosAdminPassword,
            LocalCerbosAdminPasswordHash)
        .WaitFor(fullLocalResources.Keycloak)
        .WaitFor(fullLocalResources.Cerbos)
        .WaitFor(fullLocalResources.Minio);
}

// Service discovery (via WithReference) automatically resolves the API URL at runtime.
// Do NOT hardcode ExploreAPI__BaseUrl here — Aspire assigns dynamic ports.
var exploreBlazor = WithProfileSecretMode(
        builder.AddProject<Projects.Explore_Blazor>(
                "explore-blazor",
                ExcludeProjectLaunchProfile)
            .WithHttpEndpoint(name: "http"),
        runMode,
        builder.Configuration)
    .WithReference(exploreAPI)
    .WaitFor(exploreAPI)
    .WithEnvironment("Storage__Local__RootPath", localStorageRootPath);

if (migrations is not null)
{
    exploreBlazor = exploreBlazor
        .WithReference(migrations)
        .WaitForCompletion(migrations);
}

if (database is not null)
{
    exploreBlazor = exploreBlazor
        .WithReference(database, connectionName: "DefaultConnection")
        .WaitFor(database);
}

if (cache is not null)
{
    exploreBlazor = exploreBlazor
        .WithReference(cache)
        .WaitFor(cache);
}

if (fullLocalResources is not null)
{
    exploreBlazor = ConfigureFullLocalBlazor(exploreBlazor, fullLocalResources)
        .WaitFor(fullLocalResources.Keycloak);
}

await builder.Build().RunAsync();

static FullLocalResources AddFullLocalPlatform(
    IDistributedApplicationBuilder builder,
    string cerbosConfigPath,
    string cerbosPolicyPackagePath,
    string cerbosSchemaPath,
    string localCerbosAdminUsername,
    string localCerbosAdminPasswordHash,
    string keycloakRealmExportPath,
    string prometheusConfigPath,
    string grafanaDashboardPath,
    IResourceBuilder<RedisResource>? cache)
{
    var crdb = builder.AddContainer("crdb", "cockroachdb/cockroach", "v24.1.1")
        .WithArgs("start-single-node", "--insecure")
        .WithVolume("islamu-event-crdb-data", "/cockroach/cockroach-data")
        .WithEndpoint(
            targetPort: 26257,
            port: 26257,
            name: "sql",
            protocol: ProtocolType.Tcp)
        .WithHttpEndpoint(targetPort: 8080, port: 8081, name: "ui")
        .WithLifetime(ContainerLifetime.Persistent);

    var keycloak = builder.AddContainer("keycloak", "quay.io/phasetwo/phasetwo-keycloak", "26")
        .WithArgs(
            "start",
            "--import-realm",
            "--verbose",
            "--spi-email-template-provider=freemarker-plus-mustache",
            "--spi-email-template-freemarker-plus-mustache-enabled=true",
            "--spi-theme-cache-themes=false")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
        .WithEnvironment("KC_DB", "cockroach")
        .WithEnvironment("KC_DB_URL_HOST", "crdb")
        .WithEnvironment("KC_DB_URL_PORT", "26257")
        .WithEnvironment("KC_DB_URL_DATABASE", "defaultdb")
        .WithEnvironment("KC_DB_SCHEMA", "public")
        .WithEnvironment("KC_DB_USERNAME", "root")
        .WithEnvironment("KC_DB_PASSWORD", "")
        .WithEnvironment("KC_DB_URL_PROPERTIES", "?sslmode=disable&useCockroachMetadata=true")
        .WithEnvironment("KC_TRANSACTION_XA_ENABLED", "false")
        .WithEnvironment("KC_TRANSACTION_JTA_ENABLED", "false")
        .WithEnvironment("KC_CACHE_CONFIG_FILE", "cache-ispn-jdbc-ping.xml")
        .WithEnvironment("KC_ISPN_DB_VENDOR", "cockroachdb")
        .WithEnvironment("KC_HTTP_ENABLED", "true")
        .WithEnvironment("KC_HTTP_RELATIVE_PATH", "/auth")
        .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
        .WithEnvironment("KC_HOSTNAME", "http://localhost:8080/auth")
        .WithEnvironment("KC_HOSTNAME_STRICT", "false")
        .WithEnvironment("KC_HEALTH_ENABLED", "true")
        .WithEnvironment("KC_METRICS_ENABLED", "true")
        .WithEnvironment("KC_LOG_LEVEL", "INFO,io.phasetwo:DEBUG")
        .WithVolume("islamu-event-keycloak-data", "/opt/keycloak/data")
        .WithBindMount(keycloakRealmExportPath, "/opt/keycloak/data/import/realm-export.json", isReadOnly: true)
        .WithHttpEndpoint(targetPort: 8080, port: 8080, name: "http")
        .WithHttpEndpoint(targetPort: 9000, port: 9000, name: "mgmt")
        .WithHttpHealthCheck("/auth/health/ready", endpointName: "mgmt")
        .WithLifetime(ContainerLifetime.Persistent)
        .WaitFor(crdb);

    var cerbosDb = builder.AddContainer("cerbos-db", "postgres", "18-alpine")
        .WithEnvironment("POSTGRES_USER", "cerbos_user")
        .WithEnvironment("POSTGRES_PASSWORD", "cerbos_password")
        .WithEnvironment("POSTGRES_DB", "cerbos")
        .WithVolume("islamu-event-cerbos-data", "/var/lib/postgresql")
        .WithBindMount(cerbosSchemaPath, "/docker-entrypoint-initdb.d/cerbos-schema.sql", isReadOnly: true)
        .WithLifetime(ContainerLifetime.Persistent);

    var cerbos = builder.AddContainer("cerbos", "ghcr.io/cerbos/cerbos", "0.53.0")
        .WithArgs("server", "--config=/config/.cerbos.yaml")
        .WithEnvironment("CERBOS_ADMIN_USER", localCerbosAdminUsername)
        .WithEnvironment("CERBOS_ADMIN_PASSWORD_HASH", localCerbosAdminPasswordHash)
        .WithEnvironment(
            "CERBOS_PG_URL",
            "postgres://cerbos_user:cerbos_password@cerbos-db:5432/cerbos?search_path=cerbos&sslmode=disable")
        .WithBindMount(cerbosConfigPath, "/config/.cerbos.yaml", isReadOnly: true)
        .WithBindMount(cerbosPolicyPackagePath, "/policies", isReadOnly: true)
        .WithHttpEndpoint(targetPort: 3592, port: 3592, name: "http")
        .WithEndpoint(
            targetPort: 3593,
            port: 3593,
            name: "grpc",
            protocol: ProtocolType.Tcp)
        .WithHttpHealthCheck("/_cerbos/health", endpointName: "http")
        .WithLifetime(ContainerLifetime.Persistent)
        .WaitFor(cerbosDb);

    var minio = builder.AddContainer("minio", "minio/minio", "latest")
        .WithArgs("server", "/data", "--console-address", ":9001")
        .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
        .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
        .WithEnvironment("MINIO_SERVER_URL", "http://localhost:9005")
        .WithVolume("islamu-event-minio-data", "/data")
        .WithHttpEndpoint(targetPort: 9000, port: 9005, name: "api")
        .WithHttpEndpoint(targetPort: 9001, port: 9006, name: "console")
        .WithHttpHealthCheck("/minio/health/live", endpointName: "api")
        .WithLifetime(ContainerLifetime.Persistent);

    var svixDb = builder.AddContainer("svix-postgres", "postgres", "13.4")
        .WithEnvironment("POSTGRES_PASSWORD", "postgres")
        .WithEnvironment("POSTGRES_USER", "postgres")
        .WithEnvironment("POSTGRES_DB", "postgres")
        .WithVolume("islamu-event-svix-postgres-data", "/var/lib/postgresql/data")
        .WithLifetime(ContainerLifetime.Persistent);

    var svix = builder.AddContainer("svix", "svix/svix-server", "latest")
        .WithEnvironment("WAIT_FOR", "true")
        .WithEnvironment("SVIX_DB_DSN", "postgresql://postgres:postgres@svix-postgres:5432/postgres")
        .WithEnvironment("SVIX_QUEUE_TYPE", "redis")
        .WithEnvironment("SVIX_JWT_SECRET", LocalSvixJwtSecret)
        .WithHttpEndpoint(targetPort: 8071, port: 8071, name: "http")
        .WaitFor(svixDb);

    if (cache is not null)
    {
        svix = svix
            .WithEnvironment(
                "SVIX_REDIS_DSN",
                ReferenceExpression.Create($"redis://:{cache.Resource.PasswordParameter}@cache:6380"))
            .WaitFor(cache);
    }
    else
    {
        svix = svix.WithEnvironment("SVIX_REDIS_DSN", "redis://cache:6379");
    }

    var coopDb = builder.AddContainer("coop-postgres", "postgres", "18-alpine")
        .WithEnvironment("POSTGRES_USER", "coop")
        .WithEnvironment("POSTGRES_PASSWORD", "coop_password")
        .WithEnvironment("POSTGRES_DB", "coop")
        .WithVolume("islamu-event-coop-postgres-data", "/var/lib/postgresql")
        .WithLifetime(ContainerLifetime.Persistent);

    var coop = builder.AddContainer("coop", "ghcr.io/roostorg/coop-server", "latest")
        .WithEnvironment("NODE_ENV", "development")
        .WithEnvironment("OTEL_SERVICE_NAME", "coop")
        .WithEnvironment("PORT", "8080")
        .WithEnvironment("UI_URL", "http://localhost:8082")
        .WithEnvironment("SESSION_SECRET", "local-dev-coop-session-secret")
        .WithEnvironment("DATABASE_HOST", "coop-postgres")
        .WithEnvironment("DATABASE_READ_ONLY_HOST", "coop-postgres")
        .WithEnvironment("DATABASE_PORT", "5432")
        .WithEnvironment("DATABASE_USER", "coop")
        .WithEnvironment("DATABASE_PASSWORD", "coop_password")
        .WithEnvironment("DATABASE_NAME", "coop")
        .WithEnvironment("WAREHOUSE_ADAPTER", "noop")
        .WithEnvironment("ANALYTICS_ADAPTER", "noop")
        .WithEnvironment("SCYLLA_HOSTS", "coop-scylla")
        .WithEnvironment("SCYLLA_USERNAME", "cassandra")
        .WithEnvironment("SCYLLA_PASSWORD", "cassandra")
        .WithEnvironment("SCYLLA_LOCAL_DATACENTER", "datacenter1")
        .WithEnvironment("SCYLLA_SSL", "false")
        .WithHttpEndpoint(targetPort: 8080, port: 8082, name: "http")
        .WithLifetime(ContainerLifetime.Persistent)
        .WaitFor(coopDb);

    if (cache is not null)
    {
        coop = coop
            .WithEnvironment("REDIS_USE_CLUSTER", "false")
            .WithEnvironment("REDIS_HOST", "cache")
            .WithEnvironment("REDIS_PORT", "6380")
            .WithEnvironment("REDIS_PASSWORD", cache.Resource.PasswordParameter)
            .WaitFor(cache);
    }

    var (ospreyImage, ospreyTag) = ResolveImageAndTag(
        builder.Configuration["OSPREY_IMAGE"] ?? LocalOspreyImage,
        builder.Configuration["OSPREY_TAG"] ?? LocalOspreyTag);
    var osprey = builder.AddContainer("osprey", ospreyImage, ospreyTag)
        .WithEnvironment("RUST_LOG", "info")
        .WithEnvironment("OSPREY_COORDINATOR_BIDI_STREAM_PORT", "19950")
        .WithEnvironment("OSPREY_COORDINATOR_SYNC_ACTION_PORT", "19951")
        .WithEnvironment("POD_IP", "osprey")
        .WithEndpoint(targetPort: 19950, port: 19950, name: "bidi-stream", protocol: ProtocolType.Tcp)
        .WithEndpoint(targetPort: 19951, port: 19951, name: "sync-action", protocol: ProtocolType.Tcp)
        .WithLifetime(ContainerLifetime.Persistent);

    var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.2.1")
        .WithBindMount(prometheusConfigPath, "/etc/prometheus/prometheus.yaml", isReadOnly: true)
        .WithArgs("--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yaml")
        .WithHttpEndpoint(targetPort: 9090, port: 9090, name: "http")
        .WithLifetime(ContainerLifetime.Persistent);

    var grafana = builder.AddContainer("grafana", "grafana/grafana", "latest")
        .WithBindMount(grafanaDashboardPath, "/var/lib/grafana/dashboards", isReadOnly: true)
        .WithEnvironment("PROMETHEUS_ENDPOINT", "http://prometheus:9090")
        .WithHttpEndpoint(targetPort: 3000, port: 3000, name: "http")
        .WithLifetime(ContainerLifetime.Persistent)
        .WaitFor(prometheus);

    return new FullLocalResources(
        Keycloak: keycloak,
        Cerbos: cerbos,
        Minio: minio,
        Svix: svix,
        Coop: coop,
        Osprey: osprey,
        Prometheus: prometheus,
        Grafana: grafana);
}

static IResourceBuilder<ContainerResource> AddMailpit(IDistributedApplicationBuilder builder)
{
    return builder.AddContainer("mailpit", LocalMailpitImage, LocalMailpitTag)
        .WithEnvironment("MP_MAX_MESSAGES", "5000")
        .WithEnvironment("MP_DATABASE", "/data/mailpit.db")
        .WithEnvironment("MP_SMTP_AUTH_ACCEPT_ANY", "1")
        .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "1")
        .WithEnvironment("MP_DISABLE_VERSION_CHECK", "true")
        .WithVolume("islamu-event-mailpit-data", "/data")
        .WithEndpoint(
            targetPort: 1025,
            port: 1025,
            name: "smtp",
            protocol: ProtocolType.Tcp)
        .WithHttpEndpoint(targetPort: 8025, port: 8025, name: "http")
        .WithLifetime(ContainerLifetime.Persistent);
}

static void ExcludeProjectLaunchProfile(ProjectResourceOptions options)
{
    options.ExcludeLaunchProfile = true;
}

static IResourceBuilder<ProjectResource> ConfigureLocalMailpitSmtp(
    IResourceBuilder<ProjectResource> project)
{
    return project
        .WithEnvironment("MAIL_SMTP_HOST", "localhost")
        .WithEnvironment("MAIL_SMTP_PORT", "1025")
        .WithEnvironment("MAIL_SMTP_USERNAME", "")
        .WithEnvironment("MAIL_SMTP_PASSWORD", "")
        .WithEnvironment("MAIL_SMTP_ENCRYPTION", "None")
        .WithEnvironment("MAIL_SMTP_FROM_ADDRESS", "noreply@localhost")
        .WithEnvironment("MAIL_SMTP_FROM_NAME", "ISLAMU Event Dev")
        .WithEnvironment("SMTP_HOST", "localhost")
        .WithEnvironment("SMTP_PORT", "1025")
        .WithEnvironment("SMTP_USERNAME", "")
        .WithEnvironment("SMTP_PASSWORD", "")
        .WithEnvironment("SMTP_SECURITY", "None")
        .WithEnvironment("SMTP_FROM_ADDRESS", "noreply@localhost")
        .WithEnvironment("SMTP_FROM_NAME", "ISLAMU Event Dev");
}

static IResourceBuilder<ProjectResource> ConfigureFullLocalApi(
    IResourceBuilder<ProjectResource> api,
    FullLocalResources resources,
    string localCerbosAdminUsername,
    string localCerbosAdminPassword,
    string localCerbosAdminPasswordHash)
{
    api = api
        .WithEnvironment("KEYCLOAK_REALM", "ISLAMU")
        .WithEnvironment("KEYCLOAK_ENDPOINT", "http://localhost:8080/auth")
        .WithEnvironment("Keycloak__Realm", "ISLAMU")
        .WithEnvironment("Keycloak__Authority", "http://localhost:8080/auth/realms/ISLAMU")
        .WithEnvironment(
            "Keycloak__MetadataAddress",
            "http://localhost:8080/auth/realms/ISLAMU/.well-known/openid-configuration")
        .WithEnvironment("Keycloak__RequireHttpsMetadata", "false")
        .WithEnvironment("Keycloak__Audience", "islamu-event-api")
        .WithEnvironment("Keycloak__ValidAudiences__0", "islamu-event-api")
        .WithEnvironment("Keycloak__ValidAudiences__1", "islamu-event-blazor")
        .WithEnvironment("KeycloakBootstrap__AllowLocalUrls", "true")
        .WithEnvironment("Cerbos__GrpcEndpoint", "http://localhost:3593")
        .WithEnvironment("Cerbos__HttpEndpoint", "http://localhost:3592")
        .WithEnvironment("Cerbos__UseTls", "false")
        .WithEnvironment("Cerbos__PlaintextMode", "true")
        .WithEnvironment("Cerbos__AdminApi__Endpoints__0", "http://localhost:3592")
        .WithEnvironment("Cerbos__AdminApi__AdminUsername", localCerbosAdminUsername)
        .WithEnvironment("Cerbos__AdminApi__AdminPassword", localCerbosAdminPassword)
        .WithEnvironment("Cerbos__AdminUsername", localCerbosAdminUsername)
        .WithEnvironment("Cerbos__AdminPasswordHash", localCerbosAdminPasswordHash)
        .WithEnvironment("CERBOS_ADMIN_USERNAME", localCerbosAdminUsername)
        .WithEnvironment("CERBOS_ADMIN_PASSWORD", localCerbosAdminPassword)
        .WithEnvironment("S3Settings__Endpoint", "http://localhost:9005")
        .WithEnvironment("S3Settings__PublicEndpoint", "http://localhost:9005")
        .WithEnvironment("S3Settings__Region", "us-east-1")
        .WithEnvironment("S3Settings__BucketName", "explore")
        .WithEnvironment("S3Settings__AccessKeyId", "minioadmin")
        .WithEnvironment("S3Settings__SecretAccessKey", "minioadmin")
        .WithEnvironment("Reporting__Osprey__Enabled", "false")
        .WithEnvironment("Reporting__Osprey__AllowLocalProviderEndpoints", "true")
        .WithEnvironment("Reporting__Coop__EndpointUrl", "http://localhost:8082")
        .WithEnvironment("Reporting__Coop__AllowLocalProviderEndpoints", "true")
        .WithEnvironment("Reporting__Coop__WebhookSecret", "local-dev-coop-webhook-secret")
        .WithEnvironment("Webhooks__Svix__BaseUrl", "http://localhost:8071")
        .WithEnvironment("Webhooks__Svix__AuthTokenSecretRef", "webhooks.svix.auth_token")
        .WithEnvironment("Webhooks__Svix__OperationalWebhookSecretRef", "webhooks.svix.operational_webhook_secret")
        .WithEnvironment("WEBHOOKS_SVIX_AUTH_TOKEN", LocalSvixAuthToken)
        .WithEnvironment("WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET", LocalSvixOperationalWebhookSecret);

    api = api
        .WaitFor(resources.Svix)
        .WaitFor(resources.Coop);

    return api;
}

static IResourceBuilder<ProjectResource> ConfigureFullLocalBlazor(
    IResourceBuilder<ProjectResource> blazor,
    FullLocalResources resources)
{
    return blazor
        .WithEnvironment("KEYCLOAK_REALM", "ISLAMU")
        .WithEnvironment("KEYCLOAK_ENDPOINT", "http://localhost:8080/auth")
        .WithEnvironment("KEYCLOAK_CLIENT_ID", "islamu-event-blazor")
        .WithEnvironment("KEYCLOAK_BLAZOR_CLIENT_SECRET", "islamu-event-blazor-secret")
        .WithEnvironment("Keycloak__Realm", "ISLAMU")
        .WithEnvironment("Keycloak__Authority", "http://localhost:8080/auth/realms/ISLAMU")
        .WithEnvironment(
            "Keycloak__MetadataAddress",
            "http://localhost:8080/auth/realms/ISLAMU/.well-known/openid-configuration")
        .WithEnvironment("Keycloak__ClientId", "islamu-event-blazor")
        .WithEnvironment("Keycloak__ClientSecret", "islamu-event-blazor-secret")
        .WithEnvironment("Keycloak__RequireHttpsMetadata", "false")
        .WaitFor(resources.Keycloak);
}

static IResourceBuilder<ProjectResource> WithProfileSecretMode(
    IResourceBuilder<ProjectResource> project,
    AspireRunMode runMode,
    IConfiguration configuration)
{
    project = project
        .WithEnvironment("ISLAMU_ASPIRE_MODE", runMode.ToString())
        .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

    return runMode == AspireRunMode.FullLocal
        ? project
            .WithEnvironment("SecretProvider__Provider", "None")
            .WithEnvironment("Infisical__ProjectId", "")
            .WithEnvironment("Infisical__ClientId", "")
            .WithEnvironment("Infisical__ClientSecret", "")
            .WithEnvironment("SecretProvider__Infisical__ProjectId", "")
            .WithEnvironment("SecretProvider__Infisical__ClientId", "")
            .WithEnvironment("SecretProvider__Infisical__ClientSecret", "")
        : WithInfisicalBootstrapConfiguration(project, configuration);
}

static IResourceBuilder<ProjectResource> WithInfisicalBootstrapConfiguration(
    IResourceBuilder<ProjectResource> project,
    IConfiguration configuration)
{
    var infisical = ReadInfisicalBootstrap(configuration);

    return project
        .WithEnvironment("SecretProvider__Provider", "None")
        .WithEnvironment("Infisical__Url", infisical.Url)
        .WithEnvironment("Infisical__ProjectId", infisical.ProjectId)
        .WithEnvironment("Infisical__ClientId", infisical.ClientId)
        .WithEnvironment("Infisical__ClientSecret", infisical.ClientSecret)
        .WithEnvironment("Infisical__Environment", infisical.Environment);
}

static InfisicalBootstrapSettings ReadInfisicalBootstrap(IConfiguration configuration)
{
    static string Read(
        IConfiguration configuration,
        string key,
        string? fallback = null) =>
        configuration[$"Infisical:{key}"]
        ?? configuration[$"SecretProvider:Infisical:{key}"]
        ?? fallback
        ?? string.Empty;

    return new InfisicalBootstrapSettings(
        Url: Read(configuration, "Url", "https://app.infisical.com"),
        ProjectId: Read(configuration, "ProjectId"),
        ClientId: Read(configuration, "ClientId"),
        ClientSecret: Read(configuration, "ClientSecret"),
        Environment: Read(configuration, "Environment", "dev"));
}

static string FindRepositoryRoot(string startDirectory)
{
    var current = new DirectoryInfo(startDirectory);

    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "Explore.sln"))
            && Directory.Exists(Path.Combine(current.FullName, "cerbos", "policies")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return startDirectory;
}

static (string Image, string Tag) ResolveImageAndTag(string image, string defaultTag)
{
    var trimmed = image.Trim();
    var lastSlash = trimmed.LastIndexOf('/');
    var lastColon = trimmed.LastIndexOf(':');

    if (lastColon > lastSlash && lastColon < trimmed.Length - 1)
    {
        return (trimmed[..lastColon], trimmed[(lastColon + 1)..]);
    }

    return (trimmed, defaultTag);
}

internal enum AspireRunMode
{
    FullLocal,
    ExternalInfra,
    LocalDataExternalPlatform
}

internal static class AspireRunModeExtensions
{
    public static bool UsesLocalData(this AspireRunMode runMode) =>
        runMode is AspireRunMode.FullLocal or AspireRunMode.LocalDataExternalPlatform;

    public static AspireRunMode Parse(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return AspireRunMode.FullLocal;
        }

        var normalized = rawValue.Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (normalized.Equals("full", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("localfull", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("fulllocal", StringComparison.OrdinalIgnoreCase))
        {
            return AspireRunMode.FullLocal;
        }

        if (normalized.Equals("lite", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("locallite", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("externalinfra", StringComparison.OrdinalIgnoreCase))
        {
            return AspireRunMode.ExternalInfra;
        }

        if (normalized.Equals("core", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("localcore", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("localdataexternalplatform", StringComparison.OrdinalIgnoreCase))
        {
            return AspireRunMode.LocalDataExternalPlatform;
        }

        throw new InvalidOperationException(
            $"Unsupported ISLAMU_ASPIRE_MODE '{rawValue}'. Use FullLocal, ExternalInfra, or LocalDataExternalPlatform.");
    }
}

internal sealed record FullLocalResources(
    IResourceBuilder<ContainerResource> Keycloak,
    IResourceBuilder<ContainerResource> Cerbos,
    IResourceBuilder<ContainerResource> Minio,
    IResourceBuilder<ContainerResource> Svix,
    IResourceBuilder<ContainerResource> Coop,
    IResourceBuilder<ContainerResource>? Osprey,
    IResourceBuilder<ContainerResource> Prometheus,
    IResourceBuilder<ContainerResource> Grafana);

internal sealed record InfisicalBootstrapSettings(
    string Url,
    string ProjectId,
    string ClientId,
    string ClientSecret,
    string Environment);
