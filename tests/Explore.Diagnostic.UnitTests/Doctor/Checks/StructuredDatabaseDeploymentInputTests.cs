// ABOUTME: Verifies deployment surfaces expose canonical structured database and authority inputs.
// ABOUTME: Prevents raw connection strings and topology-resource drift in Compose and Aspire.

using Explore.Diagnostic.Doctor;
using FluentAssertions;

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
            ("Database", "DATABASE"),
            ("TlsMode", "TLS_MODE"),
            ("TrustServerCertificate", "TRUST_SERVER_CERTIFICATE"),
            ("ServerFlavor", "SERVER_FLAVOR"),
            ("ServerVersion", "SERVER_VERSION"),
        ];
        foreach (var key in sharedKeys)
        {
            compose.Should().Contain($"Database__{key.Configuration}:");
            environmentExample.Should().Contain($"DATABASE_{key.Environment}=");
        }

        compose.Should().Contain("Database__Runtime__Username:");
        compose.Should().Contain("Database__Runtime__Password:");
        compose.Should().Contain("Database__Migrator__Username:");
        compose.Should().Contain("Database__Migrator__Password:");
        environmentExample.Should().Contain("DATABASE_RUNTIME_USERNAME=");
        environmentExample.Should().Contain("DATABASE_RUNTIME_PASSWORD=");
        environmentExample.Should().Contain("DATABASE_MIGRATOR_USERNAME=");
        environmentExample.Should().Contain("DATABASE_MIGRATOR_PASSWORD=");

        compose.Should().Contain("PrivacyErasureAuthorityDatabase__Provider:");
        compose.Should().Contain("PrivacyErasureAuthorityDatabase__Host:");
        compose.Should().Contain("PrivacyErasureAuthorityDatabase__Runtime__Username:");
        compose.Should().Contain("PrivacyErasureAuthorityDatabase__Runtime__Password:");
        compose.Should().Contain("PrivacyErasureAuthorityDatabase__Migrator__Username:");
        compose.Should().Contain("PrivacyErasureAuthorityDatabase__Migrator__Password:");
        environmentExample.Should().Contain("PRIVACY_ERASURE_AUTHORITY_HOST=");
        environmentExample.Should().Contain("PRIVACY_ERASURE_AUTHORITY_RUNTIME_USERNAME=");
        environmentExample.Should().Contain("PRIVACY_ERASURE_AUTHORITY_MIGRATOR_USERNAME=");
        environmentExample.Should().Contain("PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=EmbeddedSqlite");
        environmentExample.Should().Contain("PRIVACY_ERASURE_AUTHORITY_EMBEDDED_PATH=/app/data/privacy_erasure_authority.db");
        environmentExample.Should().Contain("PRIVACY_ERASURE_AUTHORITY_WRITER_REPLICA_COUNT=1");
        environmentExample.Should().Contain("PRIVACY_ERASURE_AUTHORITY_BUSY_TIMEOUT_SECONDS=30");

        compose.Should().Contain("PrivacyErasure__Authority__Topology: ${PRIVACY_ERASURE_AUTHORITY_TOPOLOGY:-EmbeddedSqlite}");
        compose.Should().Contain("PrivacyErasureAuthorityEmbedded__Path:");
        compose.Should().Contain("PrivacyErasureAuthorityEmbedded__WriterReplicaCount:");
        compose.Should().Contain("PrivacyErasureAuthorityEmbedded__BusyTimeoutSeconds:");
        compose.Should().Contain("privacy_erasure_authority_data:/app/data");
        compose.Should().Contain("privacy-erasure-authority-volume-init:");
        compose.Should().Contain("event-migrationservice:");
        compose.Should().NotContain("event-migrationservice:\n    profiles:");
        compose.Should().Contain("replicas: 1");
        compose.Should().NotContain("PrivacyErasureAuthorityEmbedded__Cache");
        compose.Should().NotContain("privacy_erasure_authority_data:/var/lib/postgresql");

        compose.Should().NotContain("ConnectionStrings__DefaultConnection");
        compose.Should().NotContain("ConnectionStrings__EventMigrationService");
        compose.Should().NotContain("ConnectionStrings__PrivacyErasureAuthority");
        environmentExample.Should().NotContain("DATABASE_CONNECTION_STRING=");
        environmentExample.Should().NotContain("PRIVACY_ERASURE_AUTHORITY_RUNTIME_CONNECTION_STRING=");
        environmentExample.Should().NotContain("PRIVACY_ERASURE_AUTHORITY_MIGRATOR_CONNECTION_STRING=");
    }

    [Test]
    public async Task AppHost_ProjectsStructuredInputsWithoutPrimaryConnectionStringReferences()
    {
        var appHost = await File.ReadAllTextAsync(
            Path.Combine(RepositoryRoot, "src", "Explore.AppHost", "AppHost.cs"));

        appHost.Should().Contain("\"Database__Provider\"");
        appHost.Should().Contain("PrimaryDatabaseRole.Runtime");
        appHost.Should().Contain("PrimaryDatabaseRole.Migrator");
        appHost.Should().Contain("$\"Database__{role}__\"");
        appHost.Should().NotContain("WithReference(database, connectionName:");
        appHost.Should().Contain("PrivacyErasureAuthorityDatabase__Provider");
        appHost.Should().Contain("WithLocalPrivacyErasureAuthorityDatabase");
        appHost.Should().Contain("WithExternalPrivacyErasureAuthorityDatabase");
        appHost.Should().Contain("PrivacyErasureAuthorityTopology.EmbeddedSqlite");
        appHost.Should().Contain("WithEmbeddedPrivacyErasureAuthority");
        appHost.Should().Contain("/app/data/privacy_erasure_authority.db");
        appHost.Should().Contain(".WithReplicas(1)");
        appHost.Should().Contain("islamu-event-privacy-erasure-authority-data");
        appHost.Should().NotContain("connectionName: \"PrivacyErasureAuthority");
    }
}
