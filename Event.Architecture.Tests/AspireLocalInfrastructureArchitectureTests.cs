// ABOUTME: Architecture guards for local Aspire infrastructure required by integrated services.
// ABOUTME: Keeps the Osprey Kafka dependency and readiness sequencing explicit.

namespace Event.Architecture.Tests;

public sealed class AspireLocalInfrastructureArchitectureTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    [Test]
    public async Task OspreyCoordinator_MustUseReadyLocalKafkaBroker()
    {
        var appHost = await File.ReadAllTextAsync(Path.Combine(RepoRoot, "Explore.AppHost", "AppHost.cs"));

        await Assert.That(appHost).Contains("\"osprey-kafka\"");
        await Assert.That(appHost).Contains("OSPREY_KAFKA_BOOTSTRAP_SERVERS");
        await Assert.That(appHost).Contains("osprey-kafka:29092");
        await Assert.That(appHost).Contains("\"osprey-kafka-bootstrap\"");
        await Assert.That(appHost).Contains("WaitForCompletion(ospreyKafkaBootstrap)");
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Explore.sln"))
                && Directory.Exists(Path.Combine(current.FullName, "Explore.AppHost")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Explore.sln and Explore.AppHost.");
    }
}
