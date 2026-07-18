// ABOUTME: Architecture guards for local Aspire infrastructure required by integrated services.
// ABOUTME: Keeps local service endpoints and readiness sequencing explicit.

namespace Event.Architecture.Tests;

public sealed class AspireLocalInfrastructureArchitectureTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    [Test]
    public async Task OspreyCoordinator_MustUseReadyLocalKafkaBroker()
    {
        var appHost = await File.ReadAllTextAsync(Path.Combine(RepoRoot, "src", "Explore.AppHost", "AppHost.cs"));

        await Assert.That(appHost).Contains("\"osprey-kafka\"");
        await Assert.That(appHost).Contains("OSPREY_KAFKA_BOOTSTRAP_SERVERS");
        await Assert.That(appHost).Contains("osprey-kafka:29092");
        await Assert.That(appHost).Contains("\"osprey-kafka-bootstrap\"");
        await Assert.That(appHost).Contains("WaitForCompletion(ospreyKafkaBootstrap)");
    }

    [Test]
    public async Task CerbosGrpcCompatibilityEndpoint_MustUseAspireResolvedEndpoint()
    {
        var appHost = await File.ReadAllTextAsync(Path.Combine(RepoRoot, "src", "Explore.AppHost", "AppHost.cs"));

        await Assert.That(appHost).Contains(".WithEnvironment(\"CERBOS_GRPC_ENDPOINT\", cerbosGrpcEndpoint)");
    }

    [Test]
    public async Task CerbosAdminApi_MustUseMutablePostgresStore()
    {
        var cerbosConfig = await File.ReadAllTextAsync(
            Path.Combine(RepoRoot, "cerbos", "config", ".cerbos.yaml"));
        var appHost = await File.ReadAllTextAsync(Path.Combine(RepoRoot, "src", "Explore.AppHost", "AppHost.cs"));
        var compose = await File.ReadAllTextAsync(Path.Combine(RepoRoot, "docker-compose.yml"));

        await Assert.That(cerbosConfig).Contains("storage:\n  driver: \"postgres\"");
        await Assert.That(appHost).Contains("\"CERBOS_PG_URL\"");
        await Assert.That(compose).Contains("CERBOS_PG_URL:");
    }

    [Test]
    public async Task MigrationWorker_MustBeRegisteredBeforeOptionalLocalDatabaseWiring()
    {
        var appHost = await File.ReadAllTextAsync(Path.Combine(RepoRoot, "src", "Explore.AppHost", "AppHost.cs"));

        var registrationIndex = appHost.IndexOf("var migrations = WithProfileSecretMode(", StringComparison.Ordinal);
        await Assert.That(registrationIndex).IsGreaterThanOrEqualTo(0);

        var localDatabaseWiringIndex = appHost.IndexOf(
            "if (database is not null)",
            registrationIndex,
            StringComparison.Ordinal);
        await Assert.That(localDatabaseWiringIndex).IsGreaterThan(registrationIndex);
    }

    [Test]
    public async Task WebApplications_MustExposeStableHttpsEndpoints()
    {
        var appHost = await File.ReadAllTextAsync(Path.Combine(RepoRoot, "src", "Explore.AppHost", "AppHost.cs"));

        await Assert.That(appHost).Contains(".WithHttpsEndpoint(port: 7039, name: \"https\")");
        await Assert.That(appHost).Contains(".WithHttpsEndpoint(port: 7177, name: \"https\")");
    }

    [Test]
    public async Task AppHost_MustUseOnlyNamedLocalProfiles()
    {
        var launchSettings = await File.ReadAllTextAsync(
            Path.Combine(RepoRoot, "src", "Explore.AppHost", "Properties", "launchSettings.json"));
        using var document = System.Text.Json.JsonDocument.Parse(launchSettings);
        var profiles = document.RootElement.GetProperty("profiles");

        await Assert.That(profiles.TryGetProperty("local-lite", out _)).IsTrue();
        await Assert.That(profiles.TryGetProperty("local-core", out _)).IsTrue();
        await Assert.That(profiles.TryGetProperty("local-default", out _)).IsTrue();
        await Assert.That(profiles.TryGetProperty("local-full", out _)).IsTrue();
        await Assert.That(profiles.TryGetProperty("https", out _)).IsFalse();
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Explore.slnx"))
                && (Directory.Exists(Path.Combine(current.FullName, "Explore.AppHost")) ||
                    Directory.Exists(Path.Combine(current.FullName, "src", "Explore.AppHost"))))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Explore.slnx and Explore.AppHost.");
    }
}
