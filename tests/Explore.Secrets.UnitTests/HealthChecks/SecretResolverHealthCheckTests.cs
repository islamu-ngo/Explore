// ABOUTME: Unit tests for structured database validation in secret resolver readiness.
// ABOUTME: Proves invalid runtime settings fail closed without exposing credential values.

using Explore.Application.Contracts.Secrets;
using Explore.Secrets.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Explore.Secrets.UnitTests.HealthChecks;

public sealed class SecretResolverHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenRuntimeDatabaseSettingsAreInvalid_ReturnsSanitizedUnhealthyResult()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "UnsupportedProvider",
            ["Database:Host"] = "db.internal",
            ["Database:Database"] = "events",
            ["Database:Runtime:Username"] = "runtime",
            ["Database:Runtime:Password"] = "credential-canary"
        }).Build();
        var check = new SecretResolverHealthCheck(
            Substitute.For<ISecretResolver>(),
            Substitute.For<IInfisicalClientFactory>(),
            configuration,
            Substitute.For<ILogger<SecretResolverHealthCheck>>());

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Description).IsEqualTo("Secret resolver configuration is unavailable.");
        await Assert.That(result.Exception).IsNull();
        await Assert.That(result.Data["databaseConfiguration"]).IsEqualTo("invalid");
        await Assert.That(result.Description).DoesNotContain("credential-canary");
    }
}
