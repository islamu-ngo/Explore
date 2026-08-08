// ABOUTME: Verifies topology-specific privacy-erasure authority composition and connection validation.
// ABOUTME: Proves EmbeddedSqlite is the default while malformed external provider settings fail closed.

using Explore.Application.Configuration;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Persistence;
using Explore.Persistence.Privacy.ErasureAuthority;
using Explore.Persistence.Privacy.ErasureAuthority.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Privacy;

public sealed class PrivacyErasureAuthorityCompositionValidationTests
{
    [Test]
    public async Task DefaultComposition_RegistersEmbeddedAuthorityWithoutExternalDatabase()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddOptions<PrivacyErasureOptions>();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PrivacyErasureAuthorityEmbedded:Path"] =
                    Path.Combine(Path.GetTempPath(), $"authority-{Guid.CreateVersion7():N}.db"),
                ["Database:Provider"] = "PostgreSql",
                ["Database:Host"] = "unused",
                ["Database:Database"] = "unused",
                ["Database:Runtime:Username"] = "unused",
                ["Database:Runtime:Password"] = "unused"
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
            .IsTypeOf<EmbeddedPrivacyErasureAuthorityRepository>();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IDbContextFactory<EmbeddedPrivacyErasureAuthorityDbContext>)))
            .IsTrue();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
    }

    [Test]
    public async Task CoLocatedTopology_RegistersEfCoreAuthorityAdapterAgainstPrimaryDatabase()
    {
        var settings = PrimaryDatabaseSettings(new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Database = "event",
            Username = "application",
            Password = "application-canary"
        });
        settings["PrivacyErasure:Authority:Topology"] = "CoLocated";
        settings["PrivacyErasureAuthorityEmbedded:Path"] =
            Path.Combine(Path.GetTempPath(), $"authority-{Guid.CreateVersion7():N}.db");
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.ConfigurePersistenceServices(
            configuration,
            skipDbContextRegistration: true,
            skipLookupCacheInitializer: true);

        await using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        await using PrivacyErasureAuthorityDbContext context = scope.ServiceProvider.GetRequiredService<PrivacyErasureAuthorityDbContext>();

        await Assert.That(context).IsNotNull();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsTrue();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(EmbeddedPrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(EmbeddedPrivacyErasureAuthorityStorage))).IsFalse();
    }

    [Test]
    [Arguments("Port", "invalid")]
    [Arguments("TlsMode", "invalid")]
    public async Task ExternalComposition_InvalidStructuredValue_FailsBeforeRegistration(
        string field,
        string value)
    {
        var services = new ServiceCollection();
        var settings = AuthorityDatabaseSettings("authority", "privacy_erasure", "runtime", "secret");
        settings["PrivacyErasure:Authority:Topology"] = "ExternalDatabase";
        settings[$"PrivacyErasureAuthorityDatabase:{field}"] = value;
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        OptionsValidationException? exception = await Assert.That(() =>
                services.ConfigurePersistenceServices(
                    configuration,
                    skipDbContextRegistration: true,
                    skipLookupCacheInitializer: true))
            .Throws<OptionsValidationException>();

        await Assert.That(exception!.Message).DoesNotContain("secret");
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsFalse();
    }

    [Test]
    public async Task ExternalComposition_SamePhysicalApplicationDatabase_FailsBeforeRegistration()
    {
        var applicationTarget = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Database = "event",
            Username = "application",
            Password = "application-canary"
        };
        var services = new ServiceCollection();
        var settings = PrimaryDatabaseSettings(applicationTarget);
        settings["PrivacyErasure:Authority:Topology"] = "ExternalDatabase";
        foreach (var pair in AuthorityDatabaseSettings(
                     "127.0.0.1",
                     "event",
                     "runtime",
                     "authority-canary"))
        {
            settings[pair.Key] = pair.Value;
        }
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        OptionsValidationException? exception = await Assert.That(() =>
                services.ConfigurePersistenceServices(
                    configuration,
                    skipLookupCacheInitializer: true))
            .Throws<OptionsValidationException>();

        await Assert.That(exception!.Message)
            .Contains("different physical PostgreSQL database", StringComparison.OrdinalIgnoreCase);
        await Assert.That(exception.Message).DoesNotContain("application-canary");
        await Assert.That(exception.Message).DoesNotContain("authority-canary");
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(services.Any(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsFalse();
    }

    [Test]
    public async Task PersistenceComposition_RegistersExactlyOneTopologyAdapter()
    {
        ServiceCollection embedded = Compose(
            "EmbeddedSqlite",
            "event");
        ServiceCollection external = Compose(
            "ExternalDatabase",
            "authority");
        ServiceCollection coLocated = Compose(
            "CoLocated",
            "event");

        await Assert.That(embedded.Count(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsEqualTo(1);
        await Assert.That(external.Count(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsEqualTo(1);
        await Assert.That(coLocated.Count(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsEqualTo(1);
        await Assert.That(embedded.Single(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority)).ImplementationType)
            .IsEqualTo(typeof(EmbeddedPrivacyErasureAuthorityRepository));
        await Assert.That(external.Single(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority)).ImplementationType)
            .IsEqualTo(typeof(EfCorePrivacyErasureAuthorityRepository));
        await Assert.That(coLocated.Single(descriptor =>
            descriptor.ServiceType == typeof(IPrivacyErasureAuthority)).ImplementationType)
            .IsEqualTo(typeof(EfCorePrivacyErasureAuthorityRepository));
        await Assert.That(embedded.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(coLocated.Any(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsTrue();
        await Assert.That(embedded.Count(descriptor =>
            descriptor.ServiceType == typeof(IDbContextFactory<EmbeddedPrivacyErasureAuthorityDbContext>)))
            .IsEqualTo(1);
        await Assert.That(external.Count(descriptor =>
            descriptor.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsEqualTo(1);
    }

    private static ServiceCollection Compose(string topology, string authorityDatabase)
    {
        var services = new ServiceCollection();
        var settings = PrimaryDatabaseSettings(new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Database = "event",
            Username = "application",
            Password = "application-canary"
        });
        settings["PrivacyErasure:Authority:Topology"] = topology;
        settings["PrivacyErasureAuthorityEmbedded:Path"] =
            Path.Combine(Path.GetTempPath(), $"authority-{Guid.CreateVersion7():N}.db");
        foreach (var pair in AuthorityDatabaseSettings(
                     "localhost",
                     authorityDatabase,
                     "runtime",
                     "authority-canary"))
        {
            settings[pair.Key] = pair.Value;
        }
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        services.ConfigurePersistenceServices(
            configuration,
            skipDbContextRegistration: true,
            skipLookupCacheInitializer: true);
        return services;
    }

    private static Dictionary<string, string?> PrimaryDatabaseSettings(NpgsqlConnectionStringBuilder target) => new()
    {
        ["Database:Provider"] = "PostgreSql",
        ["Database:Host"] = target.Host,
        ["Database:Port"] = target.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["Database:Database"] = target.Database,
        ["Database:Runtime:Username"] = target.Username,
        ["Database:Runtime:Password"] = target.Password,
        ["Database:Migrator:Username"] = target.Username,
        ["Database:Migrator:Password"] = target.Password
    };

    private static Dictionary<string, string?> AuthorityDatabaseSettings(
        string host,
        string database,
        string username,
        string password) => new()
    {
        ["PrivacyErasureAuthorityDatabase:Provider"] = "PostgreSql",
        ["PrivacyErasureAuthorityDatabase:Host"] = host,
        ["PrivacyErasureAuthorityDatabase:Port"] = "5432",
        ["PrivacyErasureAuthorityDatabase:Database"] = database,
        ["PrivacyErasureAuthorityDatabase:TlsMode"] = "Prefer",
        ["PrivacyErasureAuthorityDatabase:TrustServerCertificate"] = "false",
        ["PrivacyErasureAuthorityDatabase:Runtime:Username"] = username,
        ["PrivacyErasureAuthorityDatabase:Runtime:Password"] = password,
        ["PrivacyErasureAuthorityDatabase:Migrator:Username"] = "migrator",
        ["PrivacyErasureAuthorityDatabase:Migrator:Password"] = "migrator-canary",
    };
}
