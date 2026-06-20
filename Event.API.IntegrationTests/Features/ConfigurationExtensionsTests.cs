// ABOUTME: Regression tests for API startup configuration compatibility mapping.
// ABOUTME: Ensures Infisical deployment keys bind to canonical .NET configuration sections.

using Explore.API.Extensions;
using Microsoft.Extensions.Configuration;

namespace Event.Api.IntegrationTests.Features;

public sealed class ConfigurationExtensionsTests
{
    [Test]
    public async Task AddInfisicalCompatibility_MapsCerbosUsePolicyScopeFromInfisicalKey()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["CERBOS_USE_POLICY_SCOPE"] = "false"
        });

        await Assert.That(configuration["Cerbos:UsePolicyScope"]).IsEqualTo("false");
    }

    [Test]
    public async Task AddInfisicalCompatibility_DoesNotOverrideCanonicalCerbosUsePolicyScope()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["CERBOS_USE_POLICY_SCOPE"] = "false",
            ["Cerbos:UsePolicyScope"] = "true"
        });

        await Assert.That(configuration["Cerbos:UsePolicyScope"]).IsEqualTo("true");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(values);

        builder.AddInfisicalCompatibility();
        return builder.Build();
    }
}
