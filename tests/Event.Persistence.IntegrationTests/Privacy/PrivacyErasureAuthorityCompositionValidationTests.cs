// ABOUTME: Verifies retained-authority composition rejects provider-invalid Npgsql connections.
// ABOUTME: Proves malformed provider settings fail closed before authority services are registered.

using Explore.Application.Configuration;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Persistence;
using Explore.Persistence.Privacy.ErasureAuthority;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Privacy;

public sealed class PrivacyErasureAuthorityCompositionValidationTests
{
    [Test]
    [Arguments("Host=localhost;Database=privacy_erasure;Username=runtime;TotallyInvalidNpgsqlKeyword=1")]
    [Arguments("Host=localhost;Database=privacy_erasure;Username=runtime;SSL Mode=DefinitelyNotAnNpgsqlValue")]
    public async Task RetainedComposition_ProviderInvalidConnection_FailsBeforeRegistration(
        string connectionString)
    {
        await Assert.That(() => new NpgsqlConnectionStringBuilder(connectionString))
            .Throws<ArgumentException>();

        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PrivacyErasure:Durability:Mode"] = "RetainedAuthority",
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
