// ABOUTME: Verifies deployment surfaces expose canonical structured database and authority inputs.
// ABOUTME: Prevents raw connection strings and topology-resource drift in Compose and Aspire.

using Explore.Diagnostic.Doctor;

namespace Explore.Diagnostic.UnitTests.Doctor.Checks;

public class StructuredDatabaseDeploymentInputTests
{
    private static readonly string RepositoryRoot = DoctorRepositoryLocator.LocateRepositoryRoot(AppContext.BaseDirectory);

    [Test]
    public async Task ComposeAndEnvironmentExample_UseCanonicalStructuredInputs()
    {
        var compose = await File.ReadAllTextAsync(Path.Combine(RepositoryRoot, "docker-compose.yml"));
        var environmentExample = await File.ReadAllTextAsync(Path.Combine(RepositoryRoot, ".env.example"));

        (string Configuration, string Environment)[] sharedKeys =
        [
            ("Provider", "PROVIDER"),
            ("Host", "HOST"),
            ("Port", "PORT"),
            ("Database", "NAME"),
            ("TlsMode", "TLS_MODE"),
            ("TrustServerCertificate", "TRUST_SERVER_CERTIFICATE"),
        ];
        foreach (var key in sharedKeys)
        {
            await Assert.That(compose).Contains($"Database__{key.Configuration}:");
            await Assert.That(environmentExample).Contains($"DATABASE_{key.Environment}=");
        }

        await Assert.That(compose).Contains("Database__Runtime__Username:");
        await Assert.That(compose).Contains("Database__Runtime__Password:");
        await Assert.That(compose).Contains("Database__Migrator__Username:");
        await Assert.That(compose).Contains("Database__Migrator__Password:");
        await Assert.That(environmentExample).Contains("DATABASE_RUNTIME_USERNAME=");
        await Assert.That(environmentExample).Contains("DATABASE_RUNTIME_PASSWORD=");
        await Assert.That(environmentExample).Contains("DATABASE_MIGRATOR_USERNAME=");
        await Assert.That(environmentExample).Contains("DATABASE_MIGRATOR_PASSWORD=");

        await Assert.That(compose).Contains("Database__Erasure__Provider:");
        await Assert.That(compose).Contains("Database__Erasure__Host:");
        await Assert.That(compose).Contains("Database__Erasure__Runtime__Username:");
        await Assert.That(compose).Contains("Database__Erasure__Runtime__Password:");
        await Assert.That(compose).Contains("Database__Erasure__Migrator__Username:");
        await Assert.That(compose).Contains("Database__Erasure__Migrator__Password:");
        await Assert.That(environmentExample).Contains("DATABASE_ERASURE_HOST=");
        await Assert.That(environmentExample).Contains("DATABASE_ERASURE_RUNTIME_USERNAME=");
        await Assert.That(environmentExample).Contains("DATABASE_ERASURE_MIGRATOR_USERNAME=");
        await Assert.That(environmentExample).Contains("ERASURE_TOPOLOGY=EmbeddedSqlite");
        await Assert.That(environmentExample).Contains("ERASURE_EMBEDDED_PATH=/app/data/privacy_erasure_authority.db");
        await Assert.That(environmentExample).Contains("ERASURE_WRITER_REPLICA_COUNT=1");
        await Assert.That(environmentExample).Contains("ERASURE_BUSY_TIMEOUT_SECONDS=30");

        await Assert.That(compose).Contains("PrivacyErasure__Authority__Topology: ${ERASURE_TOPOLOGY:-${PRIVACY_ERASURE_AUTHORITY_TOPOLOGY:-EmbeddedSqlite}}");
        await Assert.That(compose).Contains("PrivacyErasureAuthorityEmbedded__Path:");
        await Assert.That(compose).Contains("PrivacyErasureAuthorityEmbedded__WriterReplicaCount:");
        await Assert.That(compose).Contains("PrivacyErasureAuthorityEmbedded__BusyTimeoutSeconds:");
        await Assert.That(compose).Contains("privacy_erasure_authority_data:/app/data");
        await Assert.That(compose).Contains("SETUP_SECRET_FILE: ${SETUP_SECRET_FILE:-/app/bootstrap/setup-secret}");
        await Assert.That(compose).Contains("setup_data:/app/bootstrap");
        await Assert.That(compose).Contains("\n  setup_data:");
        await Assert.That(environmentExample).Contains("SETUP_SECRET=");
        await Assert.That(compose).Contains("privacy-erasure-authority-volume-init:");
        await Assert.That(compose).Contains("event-migrationservice:");
        await Assert.That(compose).DoesNotContain("event-migrationservice:\n    profiles:");
        await Assert.That(compose).Contains("replicas: 1");
        await Assert.That(compose).DoesNotContain("PrivacyErasureAuthorityEmbedded__Cache");
        await Assert.That(compose).DoesNotContain("privacy_erasure_authority_data:/var/lib/postgresql");

        await Assert.That(compose).DoesNotContain("ConnectionStrings__DefaultConnection");
        await Assert.That(compose).DoesNotContain("ConnectionStrings__EventMigrationService");
        await Assert.That(compose).DoesNotContain("ConnectionStrings__PrivacyErasureAuthority");
        await Assert.That(environmentExample).DoesNotContain("DATABASE_CONNECTION_STRING=");
        await Assert.That(environmentExample).DoesNotContain("PRIVACY_ERASURE_AUTHORITY_RUNTIME_CONNECTION_STRING=");
        await Assert.That(environmentExample).DoesNotContain("PRIVACY_ERASURE_AUTHORITY_MIGRATOR_CONNECTION_STRING=");
    }

    [Test]
    public async Task AppHost_ProjectsStructuredInputsWithoutPrimaryConnectionStringReferences()
    {
        var appHost = await File.ReadAllTextAsync(
            Path.Combine(RepositoryRoot, "src", "Explore.AppHost", "AppHost.cs"));

        await Assert.That(appHost).Contains("\"Database__Provider\"");
        await Assert.That(appHost).Contains("PrimaryDatabaseRole.Runtime");
        await Assert.That(appHost).Contains("PrimaryDatabaseRole.Migrator");
        await Assert.That(appHost).Contains("$\"Database__{role}__\"");
        await Assert.That(appHost).DoesNotContain("WithReference(database, connectionName:");
        await Assert.That(appHost).Contains("PrivacyErasureAuthorityDatabase__Provider");
        await Assert.That(appHost).Contains("WithLocalPrivacyErasureAuthorityDatabase");
        await Assert.That(appHost).Contains("WithExternalPrivacyErasureAuthorityDatabase");
        await Assert.That(appHost).Contains("PrivacyErasureAuthorityTopology.EmbeddedSqlite");
        await Assert.That(appHost).Contains("WithEmbeddedPrivacyErasureAuthority");
        await Assert.That(appHost).Contains("/app/data/privacy_erasure_authority.db");
        await Assert.That(appHost).Contains(".WithReplicas(1)");
        await Assert.That(appHost).Contains("islamu-event-privacy-erasure-authority-data");
        await Assert.That(appHost).DoesNotContain("connectionName: \"PrivacyErasureAuthority");
    }
}
