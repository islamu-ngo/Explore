// ABOUTME: Unit tests for structured database validation in secret resolver readiness.
// ABOUTME: Proves invalid runtime settings fail closed without exposing credential values.

using Explore.Application.Contracts.Secrets;
using Explore.Secrets.HealthChecks;
using FluentAssertions;
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

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Secret resolver configuration is unavailable.");
        result.Exception.Should().BeNull();
        result.Data["databaseConfiguration"].Should().Be("invalid");
        result.Description.Should().NotContain("credential-canary");
    }
}
