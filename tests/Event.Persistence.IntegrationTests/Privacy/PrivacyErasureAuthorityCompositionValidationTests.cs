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

        OptionsValidationException? exception = await Assert.That(() =>
                services.ConfigurePersistenceServices(
                    configuration,
                    skipDbContextRegistration: true,
                    skipLookupCacheInitializer: true))
            .Throws<OptionsValidationException>();

        await Assert.That(exception!.Message).DoesNotContain(connectionString);
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsFalse();
    }

    [Test]
    public async Task ExternalComposition_SamePhysicalApplicationDatabase_FailsBeforeRegistration()
    {
        const string applicationTarget =
            "Host=localhost;Database=event;Username=application;Password=application-canary";
        const string authorityTarget =
            "Application Name=authority;Username=runtime;Database=event;Port=5432;Host=127.0.0.1;Password=authority-canary";
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PrivacyErasure:Authority:Topology"] = "ExternalDatabase",
                ["ConnectionStrings:DefaultConnection"] = applicationTarget,
                ["ConnectionStrings:PrivacyErasureAuthority"] = authorityTarget
            }).Build();

        OptionsValidationException? exception = await Assert.That(() =>
                services.ConfigurePersistenceServices(
                    configuration,
                    skipDbContextRegistration: true,
                    skipLookupCacheInitializer: true))
            .Throws<OptionsValidationException>();

        await Assert.That(exception!.Message)
            .Contains("different physical PostgreSQL database", StringComparison.OrdinalIgnoreCase);
        await Assert.That(exception.Message).DoesNotContain(applicationTarget);
        await Assert.That(exception.Message).DoesNotContain(authorityTarget);
        await Assert.That(exception.Message).DoesNotContain("application-canary");
        await Assert.That(exception.Message).DoesNotContain("authority-canary");
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsFalse();
    }

    [Test]
    public async Task PersistenceComposition_RegistersExactlyOneStableMirrorAndOneTopologyAdapter()
    {
        ServiceCollection coLocated = Compose(
            "CoLocated",
            "Host=localhost;Database=event;Username=application");
        ServiceCollection external = Compose(
            "ExternalDatabase",
            "Host=localhost;Database=authority;Username=runtime");

        await Assert.That(coLocated.Count(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsEqualTo(1);
        await Assert.That(external.Count(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsEqualTo(1);
        await Assert.That(coLocated.Single(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority)).ImplementationType)
            .IsEqualTo(typeof(CoLocatedPrivacyErasureAuthorityRepository));
        await Assert.That(external.Single(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority)).ImplementationType)
            .IsEqualTo(typeof(EfCorePrivacyErasureAuthorityRepository));
        await Assert.That(coLocated.Count(descriptor =>
            descriptor.ServiceType == typeof(Explore.Application.Contracts.Persistence.IPrivacyErasureLedgerRepository)))
            .IsEqualTo(1);
        await Assert.That(external.Count(descriptor =>
            descriptor.ServiceType == typeof(Explore.Application.Contracts.Persistence.IPrivacyErasureLedgerRepository)))
            .IsEqualTo(1);
        await Assert.That(coLocated.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(external.Count(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsEqualTo(1);
    }

    private static ServiceCollection Compose(string topology, string authorityConnection)
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PrivacyErasure:Authority:Topology"] = topology,
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=event;Username=application",
                ["ConnectionStrings:PrivacyErasureAuthority"] = authorityConnection
            }).Build();
        services.ConfigurePersistenceServices(
            configuration,
            skipDbContextRegistration: true,
            skipLookupCacheInitializer: true);
        return services;
    }
}
