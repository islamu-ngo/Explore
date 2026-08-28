// ABOUTME: Specifies the canonical ConfigurationManifest startup and deployment contract.
// ABOUTME: Proves naming, sole-owner ordering, and read-only non-root artifacts without starting services.

namespace Explore.Infrastructure.Tests.ConfigurationManifest;

using System.Reflection;
using Explore.Infrastructure;

public sealed class ConfigurationManifestStartupCutoverTests
{
    private const string CanonicalModeKey = "CONFIGURATION_MANIFEST_MODE";
    private const string CanonicalPathKey = "CONFIGURATION_MANIFEST_PATH";
    private const string CanonicalPath =
        "/etc/islamu-event/bootstrap/configuration-manifest.json";

    [Test]
    public async Task InfrastructureAssembly_ExposesOnlyCanonicalStartupSurface()
    {
        Assembly assembly = typeof(InfrastructureServicesRegistration).Assembly;
        string[] expectedTypes =
        [
            "Explore.Infrastructure.ConfigurationManifest.ConfigurationManifestOptions",
            "Explore.Infrastructure.ConfigurationManifest.ConfigurationManifestStartupRunner",
            "Explore.Infrastructure.ConfigurationManifest.ConfigurationManifestPostMigrationSequence"
        ];

        await Assert.That(expectedTypes.All(name =>
                assembly.GetType(name, throwOnError: false) is not null))
            .IsTrue();
    }

    [Test]
    public async Task Options_ExposeCanonicalEnvironmentKeysAndConventionPath()
    {
        Assembly assembly = typeof(InfrastructureServicesRegistration).Assembly;
        Type optionsType = assembly.GetTypes().Single(type =>
            type.Name.EndsWith(
                "ConfigurationManifestOptions",
                StringComparison.Ordinal));

        string? modeKey = optionsType
            .GetField(
                "ModeEnvironmentVariable",
                BindingFlags.Public | BindingFlags.Static)
            ?.GetRawConstantValue() as string;
        string? pathKey = optionsType
            .GetField(
                "PathEnvironmentVariable",
                BindingFlags.Public | BindingFlags.Static)
            ?.GetRawConstantValue() as string;
        string? conventionPath = optionsType
            .GetField(
                "ConventionPath",
                BindingFlags.Public | BindingFlags.Static)
            ?.GetRawConstantValue() as string;

        await Assert.That(modeKey).IsEqualTo(CanonicalModeKey);
        await Assert.That(pathKey).IsEqualTo(CanonicalPathKey);
        await Assert.That(conventionPath).IsEqualTo(CanonicalPath);
    }

    [Test]
    public async Task DeploymentArtifacts_UseCanonicalReadOnlyNonRootContract()
    {
        string root = FindRepositoryRoot();
        string compose = await File.ReadAllTextAsync(
            Path.Combine(root, "docker-compose.yml"));
        string appHost = await File.ReadAllTextAsync(
            Path.Combine(root, "src", "Explore.AppHost", "AppHost.cs"));
        string migrationImage = await File.ReadAllTextAsync(
            Path.Combine(
                root,
                "src",
                "Event.MigrationService",
                "Dockerfile"));
        string environmentTemplate = await File.ReadAllTextAsync(
            Path.Combine(root, ".env.example"));
        string artifacts = string.Join(
            '\n',
            compose,
            appHost,
            migrationImage,
            environmentTemplate);

        await Assert.That(artifacts).Contains(CanonicalModeKey);
        await Assert.That(artifacts).Contains(CanonicalPathKey);
        await Assert.That(artifacts).Contains(CanonicalPath);
        await Assert.That(artifacts).DoesNotContain("TENANT_" + "MANIFEST_");
        await Assert.That(artifacts)
            .DoesNotContain("tenant-configuration.json");
        await Assert.That(compose).Contains("read_only: true");
        await Assert.That(migrationImage).Contains("USER $APP_UID");
    }

    [Test]
    public async Task Hosts_OrderTheSingleOwnerBeforeServingTraffic()
    {
        string root = FindRepositoryRoot();
        string appHost = await File.ReadAllTextAsync(
            Path.Combine(root, "src", "Explore.AppHost", "AppHost.cs"));
        string compose = await File.ReadAllTextAsync(
            Path.Combine(root, "docker-compose.yml"));
        string standalone = await File.ReadAllTextAsync(
            Path.Combine(root, "src", "Event.Standalone", "Program.cs"));

        await Assert.That(appHost).Contains(".WaitForCompletion(migrations)");
        await Assert.That(compose)
            .Contains("condition: service_completed_successfully");
        await Assert.That(standalone.IndexOf(
                "startupSequence.RunAsync",
                StringComparison.Ordinal)
            < standalone.IndexOf(
                "RunApiHostStartupAsync",
                StringComparison.Ordinal))
            .IsTrue();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Explore.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root.");
    }
}
