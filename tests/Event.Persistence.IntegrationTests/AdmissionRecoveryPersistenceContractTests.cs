// ABOUTME: Specifies tenant-filtered digest-only admission recovery persistence metadata.
// ABOUTME: Requires portable lineage uniqueness, concurrency, and no plaintext capability or identity fields.

using Explore.Domain;
using Explore.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Event.Persistence.IntegrationTests;

public sealed class AdmissionRecoveryPersistenceContractTests
{
    [Test]
    public async Task ModelMapsTenantQualifiedDigestOnlyRecoveryState()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ExploreDbContext(options);

        IEntityType? entity = context.Model.FindEntityType(typeof(AdmissionRecoveryCapability));
        string[] propertyNames = entity?.GetProperties().Select(property => property.Name).ToArray() ?? [];
        string[] indexNames = entity?.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray() ?? [];

        await Assert.That(entity).IsNotNull();
        await Assert.That(entity!.GetTableName()).IsEqualTo("admission_recovery_capabilities");
        await Assert.That(entity.GetQueryFilter()).IsNotNull();
        await Assert.That(entity.FindProperty(nameof(AdmissionRecoveryCapability.ConcurrencyStamp))!
            .IsConcurrencyToken).IsTrue();
        await Assert.That(indexNames).Contains("ux_admission_recovery_capabilities_digest");
        await Assert.That(indexNames).Contains("ux_admission_recovery_capabilities_generation");
        await Assert.That(indexNames).Contains("ux_admission_recovery_capabilities_active");
        await Assert.That(propertyNames.Intersect(
            new[] { "Capability", "Email", "NormalizedIdentity", "Recipient", "AdmissionCredential" },
            StringComparer.OrdinalIgnoreCase)).IsEmpty();
    }
}
