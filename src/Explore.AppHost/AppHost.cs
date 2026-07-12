// ABOUTME: .NET Aspire AppHost for profile-driven local development orchestration.
// ABOUTME: Branches full, core, and lite topologies while keeping app projects unchanged.

using System.Net.Sockets;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

const string LocalCerbosAdminPasswordHash =
    "JDJiJDEwJGxUWWVjblZpTlRseTZvUkhQS3Y5U2VKZGpwZzdqWkFRcGV2S2Ezbkxpbk55bDF5U1dEZVkyCg==";

var repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory());
var dotenvPath = Path.Combine(repositoryRoot, ".env");
if (File.Exists(dotenvPath))
    Env.NoClobber().Load(dotenvPath);
var builder = DistributedApplication.CreateBuilder(args);
var runMode = AspireRunModeExtensions.Parse(builder.Configuration["ISLAMU_ASPIRE_MODE"]);
var appHostConfigRoot = Path.Combine(repositoryRoot, "src", "Explore.AppHost", "Config");
var cerbosPolicyPackagePath = Path.Combine(repositoryRoot, "cerbos", "policies");
var cerbosConfigPath = Path.Combine(repositoryRoot, "cerbos", "config", ".cerbos.yaml");
var cerbosSchemaPath = Path.Combine(repositoryRoot, "cerbos", "init", "cerbos-schema.sql");
var keycloakRealmExportPath = Path.Combine(repositoryRoot, "docker", "keycloak", "realm-export.json");
var keycloakInitScriptPath = Path.Combine(repositoryRoot, "docker", "keycloak", "keycloak-init.sh");
var coopNginxConfigPath = Path.Combine(appHostConfigRoot, "coop", "nginx.conf");
var localStorageRootPath = Path.Combine(repositoryRoot, "storage-data", "aspire-local");
var prometheusConfigPath = Path.Combine(appHostConfigRoot, "prometheus.yaml");
var grafanaDashboardPath = Path.Combine(appHostConfigRoot, "grafana-dashboard");
var pgAdminServersPath = Path.Combine(appHostConfigRoot, "pgadmin", "servers.json");
var pgAdminPassFilePath = Path.Combine(appHostConfigRoot, "pgadmin", "pgpass");
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
        .AddDatabase("islamu-event-db", "islamu_event_db");

    cache = builder.AddRedis("cache")
        .WithDataVolume("islamu-event-redis-data");
}

if (runMode == AspireRunMode.FullLocal)
{
    messaging = builder.AddRabbitMQ("messaging")
        .WithManagementPlugin()
        .WithDataVolume("islamu-event-rabbitmq-data");

    fullLocalResources = AddFullLocalPlatform(
        builder,
        cerbosConfigPath,
        cerbosPolicyPackagePath,
        cerbosSchemaPath,
        keycloakRealmExportPath,
        keycloakInitScriptPath,
        mailpit,
        coopNginxConfigPath,
        prometheusConfigPath,
        grafanaDashboardPath,
        pgAdminServersPath,
        pgAdminPassFilePath,
        database,
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

    migrations = ConfigureLocalMailpitSmtp(migrations, mailpit, builder.Configuration);
}

var exploreAPI = WithProfileSecretMode(
        builder.AddProject<Projects.Explore_API>(
                "explore-api",
                ExcludeProjectLaunchProfile)
            .WithHttpEndpoint(name: "http"),
        runMode,
        builder.Configuration)
    .WithEnvironment("HttpsRedirection__Enabled", "false")
    .WithEnvironment("CONTROL_PLANE_PUBLIC_ORIGIN", ConfiguredValue(builder.Configuration, "CONTROL_PLANE_PUBLIC_ORIGIN", "http://admin.localhost:7002"))
    .WithEnvironment("Cerbos__PolicyPackagePath", cerbosPolicyPackagePath)
    .WithEnvironment("Storage__Local__RootPath", localStorageRootPath)
    .WithEnvironment("Storage__Local__CreateRootIfMissing", "true")
    .WithEnvironment("StorageReconciliation__Enabled", "true")
    .WithEnvironment("StorageReconciliation__DryRun", "true")
    .WaitFor(mailpit);

exploreAPI = ConfigureLocalMailpitSmtp(exploreAPI, mailpit, builder.Configuration);

var vapidPublicKey = builder.Configuration["VAPID_PUBLIC_KEY"];
var vapidPrivateKey = builder.Configuration["VAPID_PRIVATE_KEY"];
var vapidSubject = builder.Configuration["VAPID_SUBJECT"];
if (!string.IsNullOrWhiteSpace(vapidPublicKey))
    exploreAPI = exploreAPI.WithEnvironment("VAPID_PUBLIC_KEY", vapidPublicKey);
if (!string.IsNullOrWhiteSpace(vapidSubject))
    exploreAPI = exploreAPI.WithEnvironment("VAPID_SUBJECT", vapidSubject);
if (!string.IsNullOrWhiteSpace(vapidPrivateKey))
{
    var vapidPrivateKeyParameter = builder.AddParameterFromConfiguration(
        "vapid-private-key",
        "VAPID_PRIVATE_KEY",
        secret: true);
    exploreAPI = exploreAPI.WithEnvironment("VAPID_PRIVATE_KEY", vapidPrivateKeyParameter);
}

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
            builder.Configuration)
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
    .WithEnvironment("Bff__AdminHosts__0", ConfiguredValue(builder.Configuration, "CONTROL_PLANE_PUBLIC_ORIGIN", "http://admin.localhost:7002"))
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
    exploreBlazor = ConfigureFullLocalBlazor(exploreBlazor, fullLocalResources, builder.Configuration)
        .WaitFor(fullLocalResources.Keycloak);
}

await builder.Build().RunAsync();

static FullLocalResources AddFullLocalPlatform(
    IDistributedApplicationBuilder builder,
    string cerbosConfigPath,
    string cerbosPolicyPackagePath,
    string cerbosSchemaPath,
    string keycloakRealmExportPath,
    string keycloakInitScriptPath,
    IResourceBuilder<ContainerResource> mailpit,
    string coopNginxConfigPath,
    string prometheusConfigPath,
    string grafanaDashboardPath,
    string pgAdminServersPath,
    string pgAdminPassFilePath,
    IResourceBuilder<PostgresDatabaseResource>? appDatabase,
    IResourceBuilder<RedisResource>? cache)
{
    var configuration = builder.Configuration;
    var cerbosAdminUsername = ConfiguredValue(configuration, "CERBOS_ADMIN_USERNAME", "cerbos");
    var cerbosAdminPasswordHash = ConfiguredValue(
        configuration,
        "CERBOS_ADMIN_PASSWORD_HASH",
        LocalCerbosAdminPasswordHash);
    var cerbosPostgresUser = configuration["CERBOS_POSTGRES_USER"] ?? "cerbos_user";
    var cerbosPostgresPassword = configuration["CERBOS_POSTGRES_PASSWORD"] ?? "cerbos_password";
    var cerbosPostgresDatabase = configuration["CERBOS_POSTGRES_DB"] ?? "cerbos";
    var crdb = builder.AddContainer("crdb", "cockroachdb/cockroach", "v24.1.1")
        .WithArgs("start-single-node", "--insecure")
        .WithVolume("islamu-event-crdb-data", "/cockroach/cockroach-data")
        .WithEndpoint(
            targetPort: 26257,
            port: 26257,
            name: "sql",
            protocol: ProtocolType.Tcp)
        .WithHttpEndpoint(targetPort: 8080, port: 8081, name: "ui");

    var keycloak = builder.AddContainer("keycloak", "quay.io/phasetwo/phasetwo-keycloak", "26")
        .WithArgs(
            "start",
            "--import-realm",
            "--verbose",
            "--spi-email-template-provider=freemarker-plus-mustache",
            "--spi-email-template-freemarker-plus-mustache-enabled=true",
            "--spi-theme-cache-themes=false")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", configuration["KEYCLOAK_ADMIN"] ?? "admin")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", configuration["KEYCLOAK_ADMIN_PASSWORD"] ?? "admin")
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
        .WithEnvironment("KC_HOSTNAME_STRICT", "false")
        .WithEnvironment("KC_HEALTH_ENABLED", "true")
        .WithEnvironment("KC_METRICS_ENABLED", "true")
        .WithEnvironment("KC_LOG_LEVEL", "INFO,io.phasetwo:DEBUG")
        .WithVolume("islamu-event-keycloak-data", "/opt/keycloak/data")
        .WithBindMount(keycloakRealmExportPath, "/opt/keycloak/data/import/realm-export.json", isReadOnly: true)
        .WithHttpEndpoint(targetPort: 8080, port: 8080, name: "http")
        .WithHttpEndpoint(targetPort: 9000, port: 9000, name: "mgmt")
        .WithHttpHealthCheck("/auth/health/ready", endpointName: "mgmt")
        .WaitFor(crdb);

    var keycloakInit = builder.AddContainer("keycloak-init", "quay.io/phasetwo/phasetwo-keycloak", "26")
        .WithEntrypoint("/bin/bash")
        .WithArgs("/opt/keycloak/bin/keycloak-init.sh")
        .WithEnvironment("KEYCLOAK_INTERNAL_URL", "http://keycloak:8080/auth")
        .WithEnvironment("KEYCLOAK_REALM", configuration["KEYCLOAK_REALM"] ?? "ISLAMU")
        .WithEnvironment("KEYCLOAK_ADMIN", configuration["KEYCLOAK_ADMIN"] ?? "admin")
        .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", configuration["KEYCLOAK_ADMIN_PASSWORD"] ?? "admin")
        .WithEnvironment("KEYCLOAK_BLAZOR_CLIENT_ID", configuration["KEYCLOAK_BLAZOR_CLIENT_ID"] ?? "islamu-event-blazor")
        .WithEnvironment("KEYCLOAK_BLAZOR_CLIENT_SECRET", configuration["KEYCLOAK_BLAZOR_CLIENT_SECRET"] ?? "islamu-event-blazor-secret")
        .WithEnvironment("KEYCLOAK_API_CLIENT_ID", configuration["KEYCLOAK_API_CLIENT_ID"] ?? "islamu-event-api")
        .WithEnvironment("KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET", configuration["KEYCLOAK_INIT_ALLOW_DEFAULT_LOCAL_SECRET"] ?? "true")
        .WithEnvironment("KEYCLOAK_SMTP_HOST", "mailpit")
        .WithEnvironment("KEYCLOAK_SMTP_PORT", "1025")
        .WithEnvironment("KEYCLOAK_SMTP_FROM", configuration["KEYCLOAK_SMTP_FROM"] ?? "noreply@openislamu.org")
        .WithEnvironment("KEYCLOAK_SMTP_FROM_DISPLAY_NAME", configuration["KEYCLOAK_SMTP_FROM_DISPLAY_NAME"] ?? "ISLAMU Event Dev")
        .WithEnvironment("KEYCLOAK_SMTP_AUTH", configuration["KEYCLOAK_SMTP_AUTH"] ?? "false")
        .WithEnvironment("KEYCLOAK_SMTP_SSL", configuration["KEYCLOAK_SMTP_SSL"] ?? "false")
        .WithEnvironment("KEYCLOAK_SMTP_STARTTLS", configuration["KEYCLOAK_SMTP_STARTTLS"] ?? "false")
        .WithBindMount(keycloakInitScriptPath, "/opt/keycloak/bin/keycloak-init.sh", isReadOnly: true)
        .WaitFor(keycloak)
        .WaitFor(mailpit);

    var cerbosDb = builder.AddContainer("cerbos-db", "postgres", "18-alpine")
        .WithEnvironment("POSTGRES_USER", cerbosPostgresUser)
        .WithEnvironment("POSTGRES_PASSWORD", cerbosPostgresPassword)
        .WithEnvironment("POSTGRES_DB", cerbosPostgresDatabase)
        .WithVolume("islamu-event-cerbos-data", "/var/lib/postgresql")
        .WithBindMount(cerbosSchemaPath, "/docker-entrypoint-initdb.d/cerbos-schema.sql", isReadOnly: true);

    var cerbos = builder.AddContainer("cerbos", "ghcr.io/cerbos/cerbos", "0.53.0")
        .WithArgs("server", "--config=/config/.cerbos.yaml")
        .WithEnvironment("CERBOS_ADMIN_USER", cerbosAdminUsername)
        .WithEnvironment("CERBOS_ADMIN_PASSWORD_HASH", cerbosAdminPasswordHash)
        .WithEnvironment(
            "CERBOS_PG_URL",
            $"postgres://{cerbosPostgresUser}:{cerbosPostgresPassword}@cerbos-db:5432/{cerbosPostgresDatabase}?search_path=cerbos&sslmode=disable")
        .WithBindMount(cerbosConfigPath, "/config/.cerbos.yaml", isReadOnly: true)
        .WithBindMount(cerbosPolicyPackagePath, "/policies", isReadOnly: true)
        .WithHttpEndpoint(targetPort: 3592, port: 3592, name: "http")
        .WithEndpoint(
            targetPort: 3593,
            port: 3593,
            name: "grpc",
            protocol: ProtocolType.Tcp)
        .WithHttpHealthCheck("/_cerbos/health", endpointName: "http")
        .WaitFor(cerbosDb);

    var minio = builder.AddContainer("minio", "minio/minio", "latest")
        .WithArgs("server", "/data", "--console-address", ":9001")
        .WithEnvironment("MINIO_ROOT_USER", configuration["STORAGE_S3_ACCESS_KEY_ID"] ?? "minioadmin")
        .WithEnvironment("MINIO_ROOT_PASSWORD", configuration["STORAGE_S3_SECRET_ACCESS_KEY"] ?? "minioadmin")
        .WithVolume("islamu-event-minio-data", "/data")
        .WithHttpEndpoint(targetPort: 9000, port: 9005, name: "api")
        .WithHttpEndpoint(targetPort: 9001, port: 9006, name: "console")
        .WithHttpHealthCheck("/minio/health/live", endpointName: "api");
    var minioBootstrap = builder.AddContainer("minio-bootstrap", "minio/mc", "latest")
        .WithEntrypoint("sh")
        .WithArgs(
            "-c",
            $"mc alias set local http://minio:9000 {configuration["STORAGE_S3_ACCESS_KEY_ID"] ?? "minioadmin"} {configuration["STORAGE_S3_SECRET_ACCESS_KEY"] ?? "minioadmin"} && (mc mb -p local/{configuration["STORAGE_S3_BUCKET_NAME"] ?? "explore"} || mc ls local/{configuration["STORAGE_S3_BUCKET_NAME"] ?? "explore"} >/dev/null)")
        .WaitFor(minio);

    var svixDb = builder.AddContainer("svix-postgres", "postgres", "13.4")
        .WithEnvironment("POSTGRES_PASSWORD", configuration["SVIX_DB_PASSWORD"] ?? "postgres")
        .WithEnvironment("POSTGRES_USER", configuration["SVIX_DB_USER"] ?? "postgres")
        .WithEnvironment("POSTGRES_DB", configuration["SVIX_DB_NAME"] ?? "postgres")
        .WithVolume("islamu-event-svix-postgres-data", "/var/lib/postgresql/data");

    var svix = builder.AddContainer("svix", "svix/svix-server", "latest")
        .WithEnvironment("WAIT_FOR", "true")
        .WithEnvironment(
            "SVIX_DB_DSN",
            $"postgresql://{configuration["SVIX_DB_USER"] ?? "postgres"}:{configuration["SVIX_DB_PASSWORD"] ?? "postgres"}@svix-postgres:5432/{configuration["SVIX_DB_NAME"] ?? "postgres"}")
        .WithEnvironment("SVIX_QUEUE_TYPE", configuration["SVIX_QUEUE_TYPE"] ?? "redis")
        .WithEnvironment("SVIX_JWT_SECRET", configuration["SVIX_JWT_SECRET"] ?? "local-dev-svix-jwt-secret-change-me")
        .WithHttpEndpoint(targetPort: 8071, port: 8071, name: "http")
        .WaitFor(svixDb);

    if (cache is not null)
    {
        svix = svix
            .WithEnvironment(
                "SVIX_REDIS_DSN",
                ReferenceExpression.Create($"redis://:{cache.Resource.PasswordParameter!}@cache:6380"))
            .WaitFor(cache);
    }
    else
    {
        svix = svix.WithEnvironment("SVIX_REDIS_DSN", "redis://cache:6379");
    }

    var weblateDb = builder.AddContainer("weblate-postgres", "postgres", "18-alpine")
        .WithEnvironment("POSTGRES_USER", configuration["WEBLATE_POSTGRES_USER"] ?? "weblate")
        .WithEnvironment("POSTGRES_PASSWORD", configuration["WEBLATE_POSTGRES_PASSWORD"] ?? "weblate_password")
        .WithEnvironment("POSTGRES_DB", configuration["WEBLATE_POSTGRES_DB"] ?? "weblate")
        .WithVolume("islamu-event-weblate-postgres-data", "/var/lib/postgresql");

    var (weblateImage, weblateTag) = ResolveImageAndTag(
        configuration["WEBLATE_IMAGE"] ?? "weblate/weblate:latest",
        configuration["WEBLATE_TAG"] ?? "latest");
    var weblate = builder.AddContainer("weblate", weblateImage, weblateTag)
        .WithEnvironment("WEBLATE_SITE_DOMAIN", configuration["WEBLATE_SITE_DOMAIN"] ?? "localhost:8083")
        .WithEnvironment("WEBLATE_ADMIN_NAME", configuration["WEBLATE_ADMIN_NAME"] ?? "Admin")
        .WithEnvironment("WEBLATE_ADMIN_EMAIL", configuration["WEBLATE_ADMIN_EMAIL"] ?? "admin@openislamu.org")
        .WithEnvironment("WEBLATE_ADMIN_PASSWORD", configuration["WEBLATE_ADMIN_PASSWORD"] ?? "admin")
        .WithEnvironment("POSTGRES_HOST", "weblate-postgres")
        .WithEnvironment("POSTGRES_PORT", "5432")
        .WithEnvironment("POSTGRES_USER", configuration["WEBLATE_POSTGRES_USER"] ?? "weblate")
        .WithEnvironment("POSTGRES_PASSWORD", configuration["WEBLATE_POSTGRES_PASSWORD"] ?? "weblate_password")
        .WithEnvironment("POSTGRES_DB", configuration["WEBLATE_POSTGRES_DB"] ?? "weblate")
        .WithHttpEndpoint(targetPort: 8080, port: 8083, name: "http")
        .WithVolume("islamu-event-weblate-data", "/app/data")
        .WaitFor(weblateDb);

    if (cache is not null)
    {
        weblate = weblate
            .WithEnvironment("VALKEY_HOST", "cache")
            .WithEnvironment("VALKEY_PORT", "6380")
            .WithEnvironment("REDIS_HOST", "cache")
            .WithEnvironment("REDIS_PORT", "6380")
            .WithEnvironment("REDIS_PASSWORD", cache.Resource.PasswordParameter!)
            .WaitFor(cache);
    }

    var coopDb = builder.AddContainer("coop-postgres", "postgres", "18-alpine")
        .WithEnvironment("POSTGRES_USER", configuration["COOP_DATABASE_USER"] ?? "coop")
        .WithEnvironment("POSTGRES_PASSWORD", configuration["COOP_DATABASE_PASSWORD"] ?? "coop_password")
        .WithEnvironment("POSTGRES_DB", configuration["COOP_DATABASE_NAME"] ?? "coop")
        .WithVolume("islamu-event-coop-postgres-data", "/var/lib/postgresql");

    var (coopMigrationsImage, coopMigrationsTag) = ResolveImageAndTag(
        builder.Configuration["COOP_MIGRATIONS_IMAGE"] ?? "ghcr.io/roostorg/coop-migrations",
        "latest");
    var coopMigrations = builder.AddContainer("coop-migrations", coopMigrationsImage, coopMigrationsTag)
        .WithEntrypoint("sh")
        .WithArgs(
            "-c",
            "npm run db:create -- --db api-server-pg --env staging && npm run db:update -- --db api-server-pg --env staging")
        .WithEnvironment("DATABASE_HOST", "coop-postgres")
        .WithEnvironment("DATABASE_READ_ONLY_HOST", "coop-postgres")
        .WithEnvironment("DATABASE_PORT", "5432")
        .WithEnvironment("DATABASE_USER", builder.Configuration["COOP_DATABASE_USER"] ?? "coop")
        .WithEnvironment("DATABASE_PASSWORD", builder.Configuration["COOP_DATABASE_PASSWORD"] ?? "coop_password")
        .WithEnvironment("DATABASE_NAME", builder.Configuration["COOP_DATABASE_NAME"] ?? "coop")
        .WithEnvironment("API_SERVER_DATABASE_HOST", "coop-postgres")
        .WithEnvironment("API_SERVER_DATABASE_PORT", "5432")
        .WithEnvironment("API_SERVER_DATABASE_USER", builder.Configuration["COOP_DATABASE_USER"] ?? "coop")
        .WithEnvironment("API_SERVER_DATABASE_PASSWORD", builder.Configuration["COOP_DATABASE_PASSWORD"] ?? "coop_password")
        .WithEnvironment("API_SERVER_DATABASE_NAME", builder.Configuration["COOP_DATABASE_NAME"] ?? "coop")
        .WithEnvironment("SCYLLA_HOSTS", builder.Configuration["COOP_SCYLLA_HOSTS"] ?? "coop-scylla")
        .WithEnvironment("SCYLLA_USERNAME", builder.Configuration["COOP_SCYLLA_USERNAME"] ?? "cassandra")
        .WithEnvironment("SCYLLA_PASSWORD", builder.Configuration["COOP_SCYLLA_PASSWORD"] ?? "cassandra")
        .WithEnvironment("SCYLLA_LOCAL_DATACENTER", builder.Configuration["COOP_SCYLLA_LOCAL_DATACENTER"] ?? "datacenter1")
        .WithEnvironment("SCYLLA_SSL", builder.Configuration["COOP_SCYLLA_SSL"] ?? "false")
        .WaitFor(coopDb);

    var (coopImage, coopTag) = ResolveImageAndTag(
        builder.Configuration["COOP_IMAGE"] ?? "ghcr.io/roostorg/coop-server",
        "latest");
    var coop = builder.AddContainer("coop", coopImage, coopTag)
        .WithEnvironment("NODE_ENV", builder.Configuration["COOP_NODE_ENV"] ?? "development")
        .WithEnvironment("OTEL_SERVICE_NAME", builder.Configuration["COOP_OTEL_SERVICE_NAME"] ?? "coop")
        .WithEnvironment("PORT", "8080")
        .WithEnvironment("UI_URL", builder.Configuration["COOP_UI_URL"] ?? "http://localhost:3001")
        .WithEnvironment("SESSION_SECRET", builder.Configuration["COOP_SESSION_SECRET"] ?? "local-dev-coop-session-secret")
        .WithEnvironment("DATABASE_HOST", "coop-postgres")
        .WithEnvironment("DATABASE_READ_ONLY_HOST", "coop-postgres")
        .WithEnvironment("DATABASE_PORT", "5432")
        .WithEnvironment("DATABASE_USER", builder.Configuration["COOP_DATABASE_USER"] ?? "coop")
        .WithEnvironment("DATABASE_PASSWORD", builder.Configuration["COOP_DATABASE_PASSWORD"] ?? "coop_password")
        .WithEnvironment("DATABASE_NAME", builder.Configuration["COOP_DATABASE_NAME"] ?? "coop")
        .WithEnvironment("WAREHOUSE_ADAPTER", builder.Configuration["COOP_WAREHOUSE_ADAPTER"] ?? "noop")
        .WithEnvironment("ANALYTICS_ADAPTER", builder.Configuration["COOP_ANALYTICS_ADAPTER"] ?? "noop")
        .WithEnvironment("SCYLLA_HOSTS", builder.Configuration["COOP_SCYLLA_HOSTS"] ?? "coop-scylla")
        .WithEnvironment("SCYLLA_USERNAME", builder.Configuration["COOP_SCYLLA_USERNAME"] ?? "cassandra")
        .WithEnvironment("SCYLLA_PASSWORD", builder.Configuration["COOP_SCYLLA_PASSWORD"] ?? "cassandra")
        .WithEnvironment("SCYLLA_LOCAL_DATACENTER", builder.Configuration["COOP_SCYLLA_LOCAL_DATACENTER"] ?? "datacenter1")
        .WithEnvironment("SCYLLA_SSL", builder.Configuration["COOP_SCYLLA_SSL"] ?? "false")
        .WithHttpEndpoint(targetPort: 8080, port: 8082, name: "http")
        .WaitFor(coopDb)
        .WaitForCompletion(coopMigrations);

    if (cache is not null)
    {
        coop = coop
            .WithEnvironment("REDIS_USE_CLUSTER", "false")
            .WithEnvironment("REDIS_HOST", "cache")
            .WithEnvironment("REDIS_PORT", "6380")
            .WithEnvironment("REDIS_PASSWORD", cache.Resource.PasswordParameter!)
            .WaitFor(cache);
    }

    var (coopClientImage, coopClientTag) = ResolveImageAndTag(
        builder.Configuration["COOP_CLIENT_IMAGE"] ?? "ghcr.io/roostorg/coop-client",
        "latest");
    var coopClient = builder.AddContainer("coop-client", coopClientImage, coopClientTag)
        .WithBindMount(coopNginxConfigPath, "/etc/nginx/conf.d/default.conf", isReadOnly: true)
        .WithHttpEndpoint(targetPort: 80, port: 3001, name: "http")
        .WaitFor(coop);

    var pgAdmin = builder.AddContainer("pgadmin", "dpage/pgadmin4", "latest")
        .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@openislamu.org")
        .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "admin")
        .WithEnvironment("PGADMIN_DISABLE_POSTFIX", "true")
        .WithEnvironment("PGADMIN_SERVER_JSON_FILE", "/pgadmin4/servers.json")
        .WithEnvironment("PGADMIN_REPLACE_SERVERS_ON_STARTUP", "True")
        .WithEnvironment("PGPASS_FILE", "/pgadmin4/pgpass")
        .WithBindMount(pgAdminServersPath, "/pgadmin4/servers.json", isReadOnly: true)
        .WithBindMount(pgAdminPassFilePath, "/pgadmin4/pgpass", isReadOnly: true)
        .WithVolume("islamu-event-pgadmin-data", "/var/lib/pgadmin")
        .WithHttpEndpoint(targetPort: 80, port: 5050, name: "http")
        .WaitFor(cerbosDb)
        .WaitFor(svixDb)
        .WaitFor(weblateDb)
        .WaitFor(coopDb);

    if (appDatabase is not null)
    {
        pgAdmin = pgAdmin.WaitFor(appDatabase);
    }

    var ospreyKafka = builder.AddContainer("osprey-kafka", "confluentinc/cp-kafka", "7.4.0")
        .WithEnvironment("KAFKA_NODE_ID", "1")
        .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
        .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@osprey-kafka:29093")
        .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
        .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "INTERNAL")
        .WithEnvironment("KAFKA_LISTENERS", "INTERNAL://osprey-kafka:29092,CONTROLLER://osprey-kafka:29093")
        .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "INTERNAL://osprey-kafka:29092")
        .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "INTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT")
        .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
        .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
        .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
        .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
        .WithEnvironment("CLUSTER_ID", "P45WxmmWSe2CrdGoeJMcKg");

    var ospreyKafkaBootstrap = builder.AddContainer("osprey-kafka-bootstrap", "confluentinc/cp-kafka", "7.4.0")
        .WithEntrypoint("bash")
        .WithArgs(
            "-c",
            "until kafka-topics --bootstrap-server osprey-kafka:29092 --list >/dev/null 2>&1; do sleep 1; done; kafka-topics --bootstrap-server osprey-kafka:29092 --create --if-not-exists --topic osprey.actions_input --partitions 3 --replication-factor 1")
        .WaitFor(ospreyKafka);

    var (ospreyImage, ospreyTag) = ResolveImageAndTag(
        configuration["OSPREY_IMAGE"] ?? "ghcr.io/roostorg/osprey/osprey-coordinator:latest",
        configuration["OSPREY_TAG"] ?? "latest");
    var osprey = builder.AddContainer("osprey", ospreyImage, ospreyTag)
        .WithEnvironment("RUST_LOG", configuration["OSPREY_RUST_LOG"] ?? "info")
        .WithEnvironment("OSPREY_COORDINATOR_CONSUMER_TYPE", "kafka")
        .WithEnvironment("OSPREY_KAFKA_BOOTSTRAP_SERVERS", "osprey-kafka:29092")
        .WithEnvironment("OSPREY_KAFKA_INPUT_STREAM_TOPIC", "osprey.actions_input")
        .WithEnvironment("OSPREY_KAFKA_GROUP_ID", "osprey_coordinator_group")
        .WithEnvironment("OSPREY_COORDINATOR_BIDI_STREAM_PORT", "19950")
        .WithEnvironment("OSPREY_COORDINATOR_SYNC_ACTION_PORT", "19951")
        .WithEnvironment("POD_IP", "osprey")
        .WithEndpoint(targetPort: 19950, port: 19950, name: "bidi-stream", protocol: ProtocolType.Tcp)
        .WithEndpoint(targetPort: 19951, port: 19951, name: "sync-action", protocol: ProtocolType.Tcp)
        .WaitForCompletion(ospreyKafkaBootstrap);

    var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.2.1")
        .WithBindMount(prometheusConfigPath, "/etc/prometheus/prometheus.yaml", isReadOnly: true)
        .WithArgs("--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yaml")
        .WithHttpEndpoint(targetPort: 9090, port: 9090, name: "http")
        .WithVolume("islamu-event-prometheus-data", "/prometheus");

    var grafana = builder.AddContainer("grafana", "grafana/grafana", "latest")
        .WithBindMount(grafanaDashboardPath, "/var/lib/grafana/dashboards", isReadOnly: true)
        .WithEnvironment("PROMETHEUS_ENDPOINT", "http://prometheus:9090")
        .WithHttpEndpoint(targetPort: 3000, port: 3000, name: "http")
        .WithVolume("islamu-event-grafana-data", "/var/lib/grafana")
        .WaitFor(prometheus);

    return new FullLocalResources(
        Keycloak: keycloak,
        KeycloakInit: keycloakInit,
        Cerbos: cerbos,
        Minio: minio,
        MinioBootstrap: minioBootstrap,
        Svix: svix,
        WeblateDb: weblateDb,
        Weblate: weblate,
        Coop: coop,
        CoopMigrations: coopMigrations,
        CoopClient: coopClient,
        PgAdmin: pgAdmin,
        Osprey: osprey,
        Prometheus: prometheus,
        Grafana: grafana);
}

static IResourceBuilder<ContainerResource> AddMailpit(IDistributedApplicationBuilder builder)
{
    return builder.AddContainer("mailpit", "axllent/mailpit", builder.Configuration["MAILPIT_TAG"] ?? "latest")
        .WithEnvironment("MP_MAX_MESSAGES", builder.Configuration["MAILPIT_MAX_MESSAGES"] ?? "5000")
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
        .WithHttpEndpoint(targetPort: 8025, port: 8025, name: "http");
}

static void ExcludeProjectLaunchProfile(ProjectResourceOptions options)
{
    options.ExcludeLaunchProfile = true;
}

static IResourceBuilder<ProjectResource> ConfigureLocalMailpitSmtp(
    IResourceBuilder<ProjectResource> project,
    IResourceBuilder<ContainerResource> mailpit,
    IConfiguration configuration)
{
    var smtpHost = EndpointHost(mailpit, "smtp");
    var smtpPort = EndpointPort(mailpit, "smtp");

    return project
        .WithEnvironment("MAIL_SMTP_HOST", smtpHost)
        .WithEnvironment("MAIL_SMTP_PORT", smtpPort)
        .WithEnvironment("MAIL_SMTP_USERNAME", configuration["MAIL_SMTP_USERNAME"] ?? string.Empty)
        .WithEnvironment("MAIL_SMTP_PASSWORD", configuration["MAIL_SMTP_PASSWORD"] ?? string.Empty)
        .WithEnvironment("MAIL_SMTP_ENCRYPTION", configuration["MAIL_SMTP_ENCRYPTION"] ?? "None")
        .WithEnvironment("MAIL_SMTP_FROM_ADDRESS", configuration["MAIL_SMTP_FROM_ADDRESS"] ?? "noreply@localhost")
        .WithEnvironment("MAIL_SMTP_FROM_NAME", configuration["MAIL_SMTP_FROM_NAME"] ?? "ISLAMU Event Dev")
        .WithEnvironment("SMTP_HOST", smtpHost)
        .WithEnvironment("SMTP_PORT", smtpPort)
        .WithEnvironment("SMTP_USERNAME", configuration["MAIL_SMTP_USERNAME"] ?? string.Empty)
        .WithEnvironment("SMTP_PASSWORD", configuration["MAIL_SMTP_PASSWORD"] ?? string.Empty)
        .WithEnvironment("SMTP_SECURITY", configuration["MAIL_SMTP_ENCRYPTION"] ?? "None")
        .WithEnvironment("SMTP_FROM_ADDRESS", configuration["MAIL_SMTP_FROM_ADDRESS"] ?? "noreply@localhost")
        .WithEnvironment("SMTP_FROM_NAME", configuration["MAIL_SMTP_FROM_NAME"] ?? "ISLAMU Event Dev");
}

static IResourceBuilder<ProjectResource> ConfigureFullLocalApi(
    IResourceBuilder<ProjectResource> api,
    FullLocalResources resources,
    IConfiguration configuration)
{
    var keycloakRealm = configuration["KEYCLOAK_REALM"] ?? "ISLAMU";
    var keycloakApiClientId = configuration["KEYCLOAK_API_CLIENT_ID"] ?? "islamu-event-api";
    var keycloakBlazorClientId = configuration["KEYCLOAK_BLAZOR_CLIENT_ID"] ?? "islamu-event-blazor";
    var keycloakBlazorClientSecret = configuration["KEYCLOAK_BLAZOR_CLIENT_SECRET"] ?? "islamu-event-blazor-secret";
    var cerbosAdminUsername = ConfiguredValue(configuration, "CERBOS_ADMIN_USERNAME", "cerbos");
    var cerbosAdminPassword = ConfiguredValue(configuration, "CERBOS_ADMIN_PASSWORD", "cerbos");
    var cerbosAdminPasswordHash = ConfiguredValue(configuration, "CERBOS_ADMIN_PASSWORD_HASH", LocalCerbosAdminPasswordHash);
    var keycloakBaseUrl = EndpointUrl(resources.Keycloak, "http", "/auth");
    var keycloakAuthority = EndpointUrl(resources.Keycloak, "http", $"/auth/realms/{keycloakRealm}");
    var keycloakMetadataAddress = EndpointUrl(
        resources.Keycloak,
        "http",
        $"/auth/realms/{keycloakRealm}/.well-known/openid-configuration");
    var cerbosGrpcEndpoint = HttpEndpointFromHostAndPort(resources.Cerbos, "grpc");
    var cerbosHttpEndpoint = EndpointUrl(resources.Cerbos, "http");
    var minioApiEndpoint = EndpointUrl(resources.Minio, "api");
    var coopEndpoint = EndpointUrl(resources.Coop, "http");
    var svixEndpoint = EndpointUrl(resources.Svix, "http");

    api = api
        .WithEnvironment("KEYCLOAK_REALM", keycloakRealm)
        .WithEnvironment("KEYCLOAK_ENDPOINT", keycloakBaseUrl)
        .WithEnvironment("KEYCLOAK_BLAZOR_CLIENT_ID", keycloakBlazorClientId)
        .WithEnvironment("KEYCLOAK_BLAZOR_CLIENT_SECRET", keycloakBlazorClientSecret)
        .WithEnvironment("Keycloak__Realm", keycloakRealm)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__MetadataAddress", keycloakMetadataAddress)
        .WithEnvironment("Keycloak__RequireHttpsMetadata", "false")
        .WithEnvironment("Keycloak__Audience", keycloakApiClientId)
        .WithEnvironment("Keycloak__ValidAudiences__0", keycloakApiClientId)
        .WithEnvironment("Keycloak__ValidAudiences__1", keycloakBlazorClientId)
        .WithEnvironment("KeycloakBootstrap__AllowLocalUrls", "true")
        .WithEnvironment("Cerbos__GrpcEndpoint", cerbosGrpcEndpoint)
        .WithEnvironment("CERBOS_GRPC_ENDPOINT", cerbosGrpcEndpoint)
        .WithEnvironment("Cerbos__HttpEndpoint", cerbosHttpEndpoint)
        .WithEnvironment("Cerbos__UseTls", "false")
        .WithEnvironment("Cerbos__PlaintextMode", "true")
        .WithEnvironment("Cerbos__AdminApi__Endpoints__0", cerbosHttpEndpoint)
        .WithEnvironment("Cerbos__AdminApi__AdminUsername", cerbosAdminUsername)
        .WithEnvironment("Cerbos__AdminApi__AdminPassword", cerbosAdminPassword)
        .WithEnvironment("Cerbos__AdminUsername", cerbosAdminUsername)
        .WithEnvironment("Cerbos__AdminPasswordHash", cerbosAdminPasswordHash)
        .WithEnvironment("CERBOS_ADMIN_USERNAME", cerbosAdminUsername)
        .WithEnvironment("CERBOS_ADMIN_PASSWORD", cerbosAdminPassword)
        .WithEnvironment("S3Settings__Endpoint", minioApiEndpoint)
        .WithEnvironment("S3Settings__PublicEndpoint", minioApiEndpoint)
        .WithEnvironment("S3Settings__Region", configuration["STORAGE_S3_REGION"] ?? "us-east-1")
        .WithEnvironment("S3Settings__BucketName", configuration["STORAGE_S3_BUCKET_NAME"] ?? "explore")
        .WithEnvironment("S3Settings__AccessKeyId", configuration["STORAGE_S3_ACCESS_KEY_ID"] ?? "minioadmin")
        .WithEnvironment("S3Settings__SecretAccessKey", configuration["STORAGE_S3_SECRET_ACCESS_KEY"] ?? "minioadmin")
        .WithEnvironment("Reporting__Enabled", configuration["REPORTING_ENABLED"] ?? "true")
        .WithEnvironment("Reporting__Mode", configuration["REPORTING_MODE"] ?? "Coop")
        .WithEnvironment("Reporting__SyncReports", configuration["REPORTING_SYNC_REPORTS"] ?? "true")
        .WithEnvironment("Reporting__EvaluateSignals", configuration["REPORTING_EVALUATE_SIGNALS"] ?? "false")
        .WithEnvironment("Reporting__MirrorReviewQueue", configuration["REPORTING_MIRROR_REVIEW_QUEUE"] ?? "true")
        .WithEnvironment("Reporting__ExecuteDecisions", configuration["REPORTING_EXECUTE_DECISIONS"] ?? "true")
        .WithEnvironment("Reporting__Osprey__Enabled", configuration["REPORTING_OSPREY_ENABLED"] ?? "false")
        .WithEnvironment("Reporting__Osprey__AllowLocalProviderEndpoints", configuration["REPORTING_OSPREY_ALLOW_LOCAL_PROVIDER_ENDPOINTS"] ?? "true")
        .WithEnvironment("Reporting__Coop__Enabled", configuration["REPORTING_COOP_ENABLED"] ?? "true")
        .WithEnvironment("Reporting__Coop__EndpointUrl", coopEndpoint)
        .WithEnvironment("Reporting__Coop__ApiKey", configuration["REPORTING_COOP_API_KEY"] ?? "local-dev-coop-api-key")
        .WithEnvironment("Reporting__Coop__AllowLocalProviderEndpoints", configuration["REPORTING_COOP_ALLOW_LOCAL_PROVIDER_ENDPOINTS"] ?? "true")
        .WithEnvironment("Reporting__Coop__WebhookSecret", configuration["REPORTING_COOP_WEBHOOK_SECRET"] ?? "local-dev-coop-webhook-secret")
        .WithEnvironment("Webhooks__Enabled", configuration["WEBHOOKS_ENABLED"] ?? "true")
        .WithEnvironment("Webhooks__Provider", configuration["WEBHOOKS_PROVIDER"] ?? "Svix")
        .WithEnvironment("Webhooks__Svix__BaseUrl", svixEndpoint)
        .WithEnvironment("Webhooks__Svix__AuthTokenSecretRef", "webhooks.svix.auth_token")
        .WithEnvironment("Webhooks__Svix__OperationalWebhookSecretRef", "webhooks.svix.operational_webhook_secret")
        .WithEnvironment("WEBHOOKS_SVIX_AUTH_TOKEN", configuration["WEBHOOKS_SVIX_AUTH_TOKEN"] ?? string.Empty)
        .WithEnvironment("WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET", configuration["WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET"] ?? string.Empty);

    api = api
        .WaitFor(resources.Svix)
        .WaitFor(resources.Weblate)
        .WaitFor(resources.Coop)
        .WaitForCompletion(resources.KeycloakInit)
        .WaitForCompletion(resources.MinioBootstrap);

    return api;
}

static IResourceBuilder<ProjectResource> ConfigureFullLocalBlazor(
    IResourceBuilder<ProjectResource> blazor,
    FullLocalResources resources,
    IConfiguration configuration)
{
    var keycloakRealm = configuration["KEYCLOAK_REALM"] ?? "ISLAMU";
    var keycloakClientId = configuration["KEYCLOAK_BLAZOR_CLIENT_ID"] ?? "islamu-event-blazor";
    var keycloakClientSecret = configuration["KEYCLOAK_BLAZOR_CLIENT_SECRET"] ?? "islamu-event-blazor-secret";
    var keycloakBaseUrl = EndpointUrl(resources.Keycloak, "http", "/auth");
    var keycloakAuthority = EndpointUrl(resources.Keycloak, "http", $"/auth/realms/{keycloakRealm}");
    var keycloakMetadataAddress = EndpointUrl(
        resources.Keycloak,
        "http",
        $"/auth/realms/{keycloakRealm}/.well-known/openid-configuration");

    return blazor
        .WithEnvironment("KEYCLOAK_REALM", keycloakRealm)
        .WithEnvironment("KEYCLOAK_ENDPOINT", keycloakBaseUrl)
        .WithEnvironment("KEYCLOAK_CLIENT_ID", keycloakClientId)
        .WithEnvironment("KEYCLOAK_BLAZOR_CLIENT_SECRET", keycloakClientSecret)
        .WithEnvironment("Keycloak__Realm", keycloakRealm)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__MetadataAddress", keycloakMetadataAddress)
        .WithEnvironment("Keycloak__ClientId", keycloakClientId)
        .WithEnvironment("Keycloak__ClientSecret", keycloakClientSecret)
        .WithEnvironment("Keycloak__RequireHttpsMetadata", "false")
        .WaitFor(resources.Keycloak)
        .WaitForCompletion(resources.KeycloakInit);
}

static ReferenceExpression EndpointUrl(
    IResourceBuilder<ContainerResource> resource,
    string endpointName,
    string path = "")
{
    var endpoint = resource.GetEndpoint(endpointName);
    return ReferenceExpression.Create($"{endpoint.Property(EndpointProperty.Url)}{path}");
}

static ReferenceExpression EndpointHost(
    IResourceBuilder<ContainerResource> resource,
    string endpointName)
{
    var endpoint = resource.GetEndpoint(endpointName);
    return ReferenceExpression.Create($"{endpoint.Property(EndpointProperty.Host)}");
}

static ReferenceExpression EndpointPort(
    IResourceBuilder<ContainerResource> resource,
    string endpointName)
{
    var endpoint = resource.GetEndpoint(endpointName);
    return ReferenceExpression.Create($"{endpoint.Property(EndpointProperty.Port)}");
}

static ReferenceExpression HttpEndpointFromHostAndPort(
    IResourceBuilder<ContainerResource> resource,
    string endpointName)
{
    var endpoint = resource.GetEndpoint(endpointName);
    return ReferenceExpression.Create($"http://{endpoint.Property(EndpointProperty.HostAndPort)}");
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
        if ((File.Exists(Path.Combine(current.FullName, "Explore.slnx")) || File.Exists(Path.Combine(current.FullName, "Explore.sln")))
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

static string ConfiguredValue(IConfiguration configuration, string key, string fallback) =>
    string.IsNullOrWhiteSpace(configuration[key]) ? fallback : configuration[key]!;

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
    IResourceBuilder<ContainerResource> KeycloakInit,
    IResourceBuilder<ContainerResource> Cerbos,
    IResourceBuilder<ContainerResource> Minio,
    IResourceBuilder<ContainerResource> MinioBootstrap,
    IResourceBuilder<ContainerResource> Svix,
    IResourceBuilder<ContainerResource> WeblateDb,
    IResourceBuilder<ContainerResource> Weblate,
    IResourceBuilder<ContainerResource> Coop,
    IResourceBuilder<ContainerResource> CoopMigrations,
    IResourceBuilder<ContainerResource> CoopClient,
    IResourceBuilder<ContainerResource> PgAdmin,
    IResourceBuilder<ContainerResource>? Osprey,
    IResourceBuilder<ContainerResource> Prometheus,
    IResourceBuilder<ContainerResource> Grafana);

internal sealed record InfisicalBootstrapSettings(
    string Url,
    string ProjectId,
    string ClientId,
    string ClientSecret,
    string Environment);
