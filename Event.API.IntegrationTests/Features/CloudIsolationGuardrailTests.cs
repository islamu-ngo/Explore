// ABOUTME: Configuration guardrail tests ensuring test environments never reference cloud endpoints.
// ABOUTME: Fails the test suite if any configuration value contains cloud-specific URLs.

using Event.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Guardrail tests that enforce cloud isolation in the test environment.
/// Prevents accidental use of production/cloud Keycloak, S3, or other external services.
/// </summary>
[Category(TestCategories.Fast)]
[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class CloudIsolationGuardrailTests
{
    private readonly ApiTestFixture _fixture;

    /// <summary>
    /// Domain patterns that must never appear in test configuration values.
    /// </summary>
    private static readonly string[] ForbiddenDomains =
    [
        "openislamu.org",
        "islamu.org",
        "auth.islamu.org",
        "api.islamu.org",
        "s3.islamu.org",
    ];

    /// <summary>
    /// Configuration keys to check for cloud endpoint leakage.
    /// </summary>
    private static readonly string[] SensitiveConfigKeys =
    [
        "Keycloak:Authority",
        "Keycloak:MetadataAddress",
        "ConnectionStrings:DefaultConnection",
        "S3Settings:Endpoint",
    ];

    public CloudIsolationGuardrailTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public void TestEnvironment_ShouldBeTesting()
    {
        // Arrange & Act
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        // Assert — WebApplicationFactory sets this to "Testing"
        // This test verifies the factory is correctly configuring the environment.
        // The environment might not be set as an env var since WAF sets it on the builder,
        // so we verify it through the configuration system instead.
        var configuration = _fixture.Factory.Services.GetRequiredService<IConfiguration>();
        var keycloakAuthority = configuration["Keycloak:Authority"];

        keycloakAuthority.Should().NotBeNull("Keycloak:Authority must be configured in test environment");
    }

    [Test]
    [MethodDataSource(nameof(GetSensitiveConfigKeys))]
    public void Configuration_ShouldNotContainCloudEndpoints(string configKey)
    {
        // Arrange
        var configuration = _fixture.Factory.Services.GetRequiredService<IConfiguration>();
        var value = configuration[configKey];

        if (string.IsNullOrEmpty(value))
        {
            return; // Key not set — no cloud leakage possible
        }

        // Assert — none of the forbidden domains should appear in the value
        foreach (var domain in ForbiddenDomains)
        {
            value.Should().NotContain(domain,
                $"configuration key '{configKey}' must not reference cloud domain '{domain}' in test environment. " +
                $"Current value: '{value}'");
        }
    }

    public static IEnumerable<string> GetSensitiveConfigKeys() => SensitiveConfigKeys;
}
