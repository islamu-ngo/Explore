// ABOUTME: Verifies topology-specific privacy-erasure authority composition and connection validation.
// ABOUTME: Proves CoLocated resolves locally while malformed external provider settings fail closed.

using Explore.Application.Configuration;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Persistence;
using Explore.Persistence.Privacy.ErasureAuthority;
using Explore.Persistence.Privacy.ErasureAuthority.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Privacy;

public sealed class PrivacyErasureAuthorityCompositionValidationTests
{
    [Test]
    public async Task CoLocatedComposition_RegistersResolvableAuthorityWithoutExternalDatabase()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PrivacyErasure:Authority:Topology"] = "CoLocated",
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=unused;Database=unused;Username=unused",
                ["ConnectionStrings:PrivacyErasureAuthority"] = string.Empty
            }).Build();

        services.ConfigurePersistenceServices(
            configuration,
            skipLookupCacheInitializer: true);

        await using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IPrivacyErasureAuthority authority =
            scope.ServiceProvider.GetRequiredService<IPrivacyErasureAuthority>();

        await Assert.That(authority)
            .IsTypeOf<CoLocatedPrivacyErasureAuthorityRepository>();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
    }

    [Test]
    [Arguments("Host=localhost;Database=privacy_erasure;Username=runtime;TotallyInvalidNpgsqlKeyword=1")]
    [Arguments("Host=localhost;Database=privacy_erasure;Username=runtime;SSL Mode=DefinitelyNotAnNpgsqlValue")]
    public async Task ExternalComposition_ProviderInvalidConnection_FailsBeforeRegistration(
        string connectionString)
    {
        await Assert.That(() => new NpgsqlConnectionStringBuilder(connectionString))
            .Throws<ArgumentException>();

        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PrivacyErasure:Authority:Topology"] = "ExternalDatabase",
                ["ConnectionStrings:PrivacyErasureAuthority"] = connectionString
            }).Build();

        OptionsValidationException exception = await Assert.That(() =>
                services.ConfigurePersistenceServices(
                    configuration,
                    skipDbContextRegistration: true,
                    skipLookupCacheInitializer: true))
            .Throws<OptionsValidationException>();

        await Assert.That(exception.Message).DoesNotContain(connectionString);
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsFalse();
    }
}
