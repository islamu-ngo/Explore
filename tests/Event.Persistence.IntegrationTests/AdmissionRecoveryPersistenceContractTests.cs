// ABOUTME: Specifies tenant-filtered digest-only admission recovery persistence metadata.
// ABOUTME: Requires portable lineage uniqueness, concurrency, and no plaintext capability or identity fields.

using Explore.Domain;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
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
        IEntityType? delivery = context.Model.FindEntityType(typeof(
            Explore.Application.Contracts.Admissions.AdmissionRecoveryDeliveryIntent));
        string[] propertyNames = entity?.GetProperties().Select(property => property.Name).ToArray() ?? [];
        string[] indexNames = entity?.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray() ?? [];

        await Assert.That(entity).IsNotNull();
        await Assert.That(entity!.GetTableName()).IsEqualTo("ie_admission_recovery_capabilities");
        await Assert.That(entity.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(entity.FindProperty(nameof(AdmissionRecoveryCapability.ConcurrencyStamp))!
            .IsConcurrencyToken).IsTrue();
        await Assert.That(entity.FindProperty(nameof(AdmissionRecoveryCapability.LookupDigest))!
            .GetTypeMapping().Converter!.ProviderClrType).IsEqualTo(typeof(byte[]));
        await Assert.That(entity.FindProperty(nameof(AdmissionRecoveryCapability.LocatorDigest))!
            .GetTypeMapping().Converter!.ProviderClrType).IsEqualTo(typeof(byte[]));
        await Assert.That(indexNames).Contains("ux_admission_recovery_capabilities_digest");
        await Assert.That(indexNames).Contains("ux_admission_recovery_capabilities_generation");
        await Assert.That(indexNames).Contains("ux_admission_recovery_capabilities_active");
        IIndex generation = entity.GetIndexes().Single(index =>
            index.GetDatabaseName() == "ux_admission_recovery_capabilities_generation");
        IIndex active = entity.GetIndexes().Single(index =>
            index.GetDatabaseName() == "ux_admission_recovery_capabilities_active");
        await Assert.That(generation.Properties.Select(property => property.Name))
            .IsEquivalentTo(
                ["TenantId", "AdmissionTicketId", "Purpose", "CapabilityVersion"]);
        await Assert.That(active.Properties.Select(property => property.Name))
            .IsEquivalentTo(
                ["TenantId", "AdmissionTicketId", "Purpose", "ActiveUniquenessSlot"]);
        await Assert.That(propertyNames.Intersect(
            new[] { "Capability", "Email", "NormalizedIdentity", "Recipient", "AdmissionCredential" },
            StringComparer.OrdinalIgnoreCase)).IsEmpty();
        await Assert.That(indexNames).Contains("ux_admission_recovery_capabilities_locator");

        string[] deliveryPropertyNames = delivery?.GetProperties()
            .Select(property => property.Name)
            .ToArray() ?? [];
        string[] deliveryIndexNames = delivery?.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray() ?? [];
        await Assert.That(delivery).IsNotNull();
        await Assert.That(delivery!.GetTableName())
            .IsEqualTo("ie_admission_recovery_delivery_intents");
        await Assert.That(delivery.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(delivery.FindProperty("ConcurrencyStamp")!.IsConcurrencyToken).IsTrue();
        await Assert.That(deliveryIndexNames)
            .Contains("ux_admission_recovery_delivery_intents_generation");
        await Assert.That(deliveryPropertyNames.Intersect(
            new[] { "Capability", "Email", "NormalizedIdentity", "Recipient", "AdmissionCredential" },
            StringComparer.OrdinalIgnoreCase)).IsEmpty();
    }
}
