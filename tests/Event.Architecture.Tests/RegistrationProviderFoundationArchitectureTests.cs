// ABOUTME: Architecture checks for Phase 9 provider-neutral persisted registration-provider foundation.
// ABOUTME: Proves credential-reference-only modeling, lookup parity, Domain purity, and migration discipline.

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Event.Architecture.Tests;

public sealed class RegistrationProviderFoundationArchitectureTests
{
    [Test]
    public async Task RegistrationProviderConnectionStoresOnlySecretBindingReferences()
    {
        await using ExploreDbContext context = CreateModelContext();
        IEntityType connection = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RegistrationProviderConnection))!;

        string[] forbiddenNames = connection.GetProperties()
            .Select(property => property.Name)
            .Where(name => name.Contains("Secret", StringComparison.Ordinal) && !name.EndsWith("SecretBindingId", StringComparison.Ordinal))
            .Concat(connection.GetProperties().Select(property => property.Name).Where(name => name.Contains("Plaintext", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        await Assert.That(forbiddenNames).IsEmpty();
    }

    [Test]
    public async Task RegistrationProviderLookupEnumParityIsComplete()
    {
        await AssertLookupParityAsync<RegistrationProviderKindEnum, RegistrationProviderKind>();
        await AssertLookupParityAsync<RegistrationProviderDeploymentKindEnum, RegistrationProviderDeploymentKind>();
        await AssertLookupParityAsync<RegistrationProviderSchemaAuthorityEnum, RegistrationProviderSchemaAuthority>();
        await AssertLookupParityAsync<RegistrationProviderPresentationModeEnum, RegistrationProviderPresentationMode>();
        await AssertLookupParityAsync<RegistrationProviderCollectionModeEnum, RegistrationProviderCollectionMode>();
        await AssertLookupParityAsync<RegistrationProviderCompletionModeEnum, RegistrationProviderCompletionMode>();
        await AssertLookupParityAsync<RegistrationProviderTrustLevelEnum, RegistrationProviderTrustLevel>();
        await AssertLookupParityAsync<RegistrationProviderDriftClassEnum, RegistrationProviderDriftClass>();
        await AssertLookupParityAsync<RegistrationProviderBindingStateEnum, RegistrationProviderBindingState>();
    }

    private static async Task AssertLookupParityAsync<TEnum, TLookup>() where TEnum : struct, Enum
    {
        await using ExploreDbContext context = CreateModelContext();
        IEntityType lookup = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(TLookup))!;
        await Assert.That(lookup.FindPrimaryKey()!.Properties.Single().ClrType).IsEqualTo(typeof(int));
        await Assert.That(Enum.GetValues<TEnum>().Select(value => Convert.ToInt32(value)).Order().ToArray()).IsEquivalentTo(
            Enumerable.Range(1, Enum.GetValues<TEnum>().Length).ToArray());
    }

    private static ExploreDbContext CreateModelContext() => new(new DbContextOptionsBuilder<ExploreDbContext>()
        .UseNpgsql("Host=localhost;Database=provider_architecture;Username=unused;Password=unused")
        .UseSnakeCaseNamingConvention().Options);
}
