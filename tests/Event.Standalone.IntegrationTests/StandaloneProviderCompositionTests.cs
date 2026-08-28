// ABOUTME: Exercises standalone persistence defaults, provider overrides, and replica safety at the composition boundary.
// ABOUTME: Uses real EF Core provider registration and a temporary SQLite file without external database infrastructure.

using Event.Standalone.Hosting;
using Event.Standalone.IntegrationTests.Fixtures;
using Explore.Persistence;
using Explore.Persistence.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Event.Standalone.IntegrationTests;

public sealed class StandaloneProviderCompositionTests
{
    [Test]
    public async Task StandaloneContainerContractUsesOneImageWithoutCompose()
    {
        var repositoryRoot = FindRepositoryRoot();
        IConfiguration settings = new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(repositoryRoot, "src", "Event.Standalone", "appsettings.json"),
                optional: false)
            .Build();
        var dockerfile = await File.ReadAllTextAsync(
            Path.Combine(repositoryRoot, "src", "Event.Standalone", "Dockerfile"));

        await Assert.That(settings["Database:Provider"]).IsEqualTo("Sqlite");
        await Assert.That(settings["Database:Database"]).IsEqualTo("/app/data/islamu_event.db");
        await Assert.That(File.Exists(Path.Combine(repositoryRoot, "docker-compose.standalone.yml"))).IsFalse();
        await Assert.That(dockerfile).Contains("EXPOSE 8080");
        await Assert.That(dockerfile).Contains("USER $APP_UID");
        await Assert.That(dockerfile).Contains("/etc/islamu-event/bootstrap");
        await Assert.That(dockerfile).Contains(
            "/app/schemas/configuration-manifest-v1alpha1.schema.json");
        await Assert.That(dockerfile).Contains("ENTRYPOINT [\"./Event.Standalone\"]");
    }

    [Test]
    public async Task StandaloneContainerRestoreIncludesBlazorFrameworkAssetsBeforeRazorSourcesAreCopied()
    {
        var dockerfile = await File.ReadAllTextAsync(
            Path.Combine(FindRepositoryRoot(), "src", "Event.Standalone", "Dockerfile"));

        var razorCopyIndex = dockerfile.IndexOf(
            "COPY [\"src/Explore.Blazor/Components/App.razor\", \"src/Explore.Blazor/Components/\"]",
            StringComparison.Ordinal);
        var restoreIndex = dockerfile.IndexOf("RUN dotnet restore", StringComparison.Ordinal);

        await Assert.That(razorCopyIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(razorCopyIndex).IsLessThan(restoreIndex);
    }

    [Test]
    public async Task StandaloneEnvironmentAllowsHttpMetadataForLocalKeycloak()
    {
        var environment = await File.ReadAllTextAsync(Path.Combine(FindRepositoryRoot(), ".env.example"));

        await Assert.That(environment).Contains("KEYCLOAK_ENDPOINT=http://keycloak.localhost:8080");
        await Assert.That(environment).Contains("Keycloak__RequireHttpsMetadata=false");
    }

    [Test]
    public async Task StandaloneSingleFileHostLoadsExternalMigrationAssembliesBeforeMigrating()
    {
        var repositoryRoot = FindRepositoryRoot();
        var program = await File.ReadAllTextAsync(
            Path.Combine(repositoryRoot, "src", "Event.Standalone", "Program.cs"));

        var loadIndex = program.IndexOf("AssemblyLoadContext.Default.LoadFromAssemblyPath", StringComparison.Ordinal);
        var migrationIndex = program.IndexOf("MigrateAndSeedAsync", StringComparison.Ordinal);

        await Assert.That(loadIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(loadIndex).IsLessThan(migrationIndex);
    }

    [Test]
    public async Task SqliteReplicaCountGreaterThanOneFailsBeforeHostStartup()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"event-standalone-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            await using var factory = new StandaloneWebApplicationFactory(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["Database:Database"] = Path.Combine(temporaryDirectory, "event.db"),
                ["Database:Host"] = null,
                ["Database:Runtime:Username"] = null,
                ["Database:Runtime:Password"] = null,
                ["Hosting:ReplicaCount"] = "2",
            });

            InvalidOperationException? exception = await Assert.That(() => factory.CreateClient())
                .Throws<InvalidOperationException>();

            await Assert.That(exception!.Message).Contains("Hosting:ReplicaCount");
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Test]
    public async Task DefaultStandaloneSqliteConfigurationUsesPersistedWalDatabaseWithThirtySecondTimeout()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"event-standalone-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        var databasePath = Path.Combine(temporaryDirectory, "event.db");

        try
        {
            using var provider = ComposeStandalonePersistence(databasePath);
            var factory = provider.GetRequiredService<IDbContextFactory<ExploreDbContext>>();
            await using var database = factory.CreateDbContext();

            await SqliteDatabaseInitializer.InitializeAsync(database, CancellationToken.None);
            var connection = database.Database.GetDbConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode;";
            var journalMode = (string?)await command.ExecuteScalarAsync();
            await connection.CloseAsync();
            var connectionString = new SqliteConnectionStringBuilder(connection.ConnectionString);

            await Assert.That(database.Database.ProviderName).IsEqualTo("Microsoft.EntityFrameworkCore.Sqlite");
            await Assert.That(connectionString.DataSource).IsEqualTo(databasePath);
            await Assert.That(connectionString.DefaultTimeout).IsEqualTo(30);
            await Assert.That(journalMode).IsEqualTo("wal");
            await Assert.That(File.Exists(databasePath)).IsTrue();
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Test]
    public async Task StructuredPostgreSqlOverrideComposesNpgsqlWithoutOpeningConnection()
    {
        using var provider = ComposePersistence(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "postgres.example.test",
            ["Database:Port"] = "5544",
            ["Database:Database"] = "event_db",
            ["Database:Schema"] = "event_schema",
            ["Database:Runtime:Username"] = "event_runtime",
            ["Database:Runtime:Password"] = "test-only-secret",
            ["Database:Runtime:TlsMode"] = "Required",
        });
        var factory = provider.GetRequiredService<IDbContextFactory<ExploreDbContext>>();
        await using var database = factory.CreateDbContext();
        var connection = new NpgsqlConnectionStringBuilder(database.Database.GetDbConnection().ConnectionString);

        await Assert.That(database.Database.ProviderName).IsEqualTo("Npgsql.EntityFrameworkCore.PostgreSQL");
        await Assert.That(connection.Host).IsEqualTo("postgres.example.test");
        await Assert.That(connection.Port).IsEqualTo(5544);
        await Assert.That(connection.Database).IsEqualTo("event_db");
        await Assert.That(database.Database.GetDbConnection().State).IsEqualTo(System.Data.ConnectionState.Closed);
    }

    [Test]
    public async Task InvalidProviderAndIncompletePostgreSqlFailDuringPersistenceRegistration()
    {
        InvalidOperationException? unknownProvider = await Assert.That(() => ComposePersistence(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Unknown",
        })).Throws<InvalidOperationException>();
        OptionsValidationException? incompletePostgreSql = await Assert.That(() => ComposePersistence(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Database"] = "event_db",
        })).Throws<OptionsValidationException>();

        await Assert.That(unknownProvider!.Message).Contains("Database:Provider must be one of");
        await Assert.That(incompletePostgreSql!.Message).Contains("PostgreSql requires Host.");
    }

    [Test]
    public async Task PostgreSqlReplicaCountGreaterThanOneStartsUnconstrained()
    {
        await using var factory = new StandaloneWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Hosting:ReplicaCount"] = "2",
        });

        using var client = factory.CreateClient();

        await Assert.That(client).IsNotNull();
    }

    private static ServiceProvider ComposeStandalonePersistence(string databasePath) =>
        ComposePersistence(new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(
                FindRepositoryRoot(),
                "src",
                "Event.Standalone",
                "appsettings.json"), optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Database"] = databasePath })
            .Build());

    private static ServiceProvider ComposePersistence(IReadOnlyDictionary<string, string?> values) =>
        ComposePersistence(new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    private static ServiceProvider ComposePersistence(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.ConfigurePersistenceServices(
            configuration,
            skipLookupCacheInitializer: true,
            environmentName: "Development");
        return services.BuildServiceProvider();
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Event.Standalone", "Dockerfile")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
